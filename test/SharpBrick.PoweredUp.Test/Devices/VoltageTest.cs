using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SharpBrick.PoweredUp.Bluetooth;
using SharpBrick.PoweredUp.Devices;
using SharpBrick.PoweredUp.Protocol;
using SharpBrick.PoweredUp.Protocol.Messages;
using Xunit;

namespace SharpBrick.PoweredUp
{
    public class VoltageTest
    {
        [Fact]
        public async Task Voltage_VoltageLObservable_Receive()
        {
            // arrange
            var (protocol, mock) = CreateProtocolAndMock();

            // announce voltage device in protocol
            await mock.WriteUpstreamAsync(new HubAttachedIOForAttachedDeviceMessage()
            {
                HubId = 0,
                PortId = 0x20,
                IOTypeId = DeviceType.Voltage,
                HardwareRevision = Version.Parse("1.0.0.0"),
                SoftwareRevision = Version.Parse("1.0.0.0"),
            });

            //await mock.WriteUpstreamAsync("0F-00-04-20-01-14-00-00-00-00-10-00-00-00-10"); 

            var voltageDevice = new Voltage(protocol, 0, 0x20);

            short actual = 0;

            using var _ = voltageDevice.VoltageLObservable.Subscribe(x => actual = x.SI);

            // assert and act
            Assert.Equal(0, actual);

            await mock.WriteUpstreamAsync("06-00-45-20-0F-00");

            Assert.Equal(35, actual);
            Assert.Equal(35, voltageDevice.VoltageL);
        }


        internal (IPoweredUpProtocol protocol, PoweredUpBluetoothCharacteristicMock mock) CreateProtocolAndMock()
        {
            var serviceProvider = new ServiceCollection()
                .AddLogging()
                .AddSingleton<PoweredUpBluetoothAdapterMock>()
                .AddSingleton<IDeviceFactory, DeviceFactory>() // for protocol knowledge init
                .BuildServiceProvider();

            var poweredUpBluetoothAdapterMock = serviceProvider.GetService<PoweredUpBluetoothAdapterMock>();

            var kernel = ActivatorUtilities.CreateInstance<BluetoothKernel>(serviceProvider, (IPoweredUpBluetoothAdapter)poweredUpBluetoothAdapterMock, (ulong)0);
            var protocol = ActivatorUtilities.CreateInstance<PoweredUpProtocol>(serviceProvider, kernel);

            protocol.ConnectAsync().Wait();

            return (protocol, poweredUpBluetoothAdapterMock.MockCharacteristic);
        }
    }
}