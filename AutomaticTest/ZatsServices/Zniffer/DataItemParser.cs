using System.Collections.Generic;
using ZWave.ZnifferApplication;

namespace Zats.Services
{
    public class DataItemParser
    {
        private readonly Dictionary<long, byte[]> _transportServiceSegments = new Dictionary<long, byte[]>();

        public SecurityManager SecurityManager { get; } = new SecurityManager();

        public void Parse(DataItem dataItem)
        {
            ParseTransportService(dataItem);
            SecurityManager.ParseSecurity(dataItem);
        }

        private void ParseTransportService(DataItem dataItem)
        {
            if (dataItem.Payload != null && dataItem.Payload.Length > 2 &&
                dataItem.Payload[0] == COMMAND_CLASS_TRANSPORT_SERVICE_V2.ID)
            {
                if ((dataItem.Payload[1] & COMMAND_CLASS_TRANSPORT_SERVICE_V2.COMMAND_FIRST_SEGMENT.ID_MASK) ==
                    COMMAND_CLASS_TRANSPORT_SERVICE_V2.COMMAND_FIRST_SEGMENT.ID ||
                    (dataItem.Payload[1] & COMMAND_CLASS_TRANSPORT_SERVICE_V2.COMMAND_SUBSEQUENT_SEGMENT.ID_MASK) ==
                    COMMAND_CLASS_TRANSPORT_SERVICE_V2.COMMAND_SUBSEQUENT_SEGMENT.ID)
                {
                    if ((dataItem.Payload[1] & COMMAND_CLASS_TRANSPORT_SERVICE_V2.COMMAND_FIRST_SEGMENT.ID_MASK) ==
                        COMMAND_CLASS_TRANSPORT_SERVICE_V2.COMMAND_FIRST_SEGMENT.ID)
                    {
                        COMMAND_CLASS_TRANSPORT_SERVICE_V2.COMMAND_FIRST_SEGMENT cmd = dataItem.Payload;
                        AddOrUpdateTransportServiceSegments(true,
                            dataItem.HomeId,
                            dataItem.Source,
                            dataItem.Destinations,
                            cmd.properties1.datagramSize1,
                            cmd.datagramSize2,
                            0,
                            0,
                            cmd.payload);
                    }
                    else if ((dataItem.Payload[1] &
                              COMMAND_CLASS_TRANSPORT_SERVICE_V2.COMMAND_SUBSEQUENT_SEGMENT.ID_MASK) ==
                             COMMAND_CLASS_TRANSPORT_SERVICE_V2.COMMAND_SUBSEQUENT_SEGMENT.ID)
                    {
                        COMMAND_CLASS_TRANSPORT_SERVICE_V2.COMMAND_SUBSEQUENT_SEGMENT cmd = dataItem.Payload;
                        var payload = AddOrUpdateTransportServiceSegments(false,
                            dataItem.HomeId,
                            dataItem.Source,
                            dataItem.Destinations,
                            cmd.properties1.datagramSize1,
                            cmd.datagramSize2,
                            cmd.properties2.datagramOffset1,
                            cmd.datagramOffset2,
                            cmd.payload);
                        if (payload != null)
                        {
                            dataItem.AddExtension(DataItemExtensionTypes.CarryPayload, payload);
                        }
                    }
                }
            }
        }

        private byte[] AddOrUpdateTransportServiceSegments(bool isFirst, IList<byte> homeId, ushort sourceId,
            IList<ushort> destination, byte datagramSize1, byte datagramSize2, byte datagramOffset1, byte datagramOffset2,
            IList<byte> payload)
        {
            byte[] ret = null;
            var datagramSize = datagramSize1 * 256 + datagramSize2;
            var datagramOffset = datagramOffset1 * 256 + datagramOffset2;
            if (destination != null && destination.Count > 0 &&
                datagramSize > 0 && datagramSize > datagramOffset &&
                payload != null && payload.Count <= datagramSize - datagramOffset)
            {
                long homeKey = 0 + (homeId[0] << 24) + (homeId[1] << 16) + (homeId[2] << 8) + homeId[3];
                long key = (homeKey << 16) + (sourceId << 8) + destination[0];

                if (isFirst)
                {
                    if (!_transportServiceSegments.ContainsKey(key))
                    {
                        _transportServiceSegments.Add(key, new byte[datagramSize * 2]);
                    }
                    else
                    {
                        _transportServiceSegments[key] = new byte[datagramSize * 2];
                    }
                    var data = _transportServiceSegments[key];
                    for (int i = 0; i < payload.Count; i++)
                    {
                        data[(datagramOffset + i) * 2] = 1;
                        data[(datagramOffset + i) * 2 + 1] = payload[i];
                    }
                }
                else if (_transportServiceSegments.ContainsKey(key))
                {
                    var data = _transportServiceSegments[key];
                    if (datagramSize * 2 == data.Length)
                    {
                        for (int i = 0; i < payload.Count; i++)
                        {
                            data[(datagramOffset + i) * 2] = 1;
                            data[(datagramOffset + i) * 2 + 1] = payload[i];
                        }
                        int sum = 0;
                        for (int i = 0; i < datagramSize; i++)
                        {
                            sum += data[i * 2];
                        }
                        if (sum == datagramSize)
                        {
                            ret = new byte[datagramSize];
                            for (int i = 0; i < datagramSize; i++)
                            {
                                ret[i] = data[i * 2 + 1];
                            }
                        }
                    }
                }
            }
            return ret;
        }
    }
}
