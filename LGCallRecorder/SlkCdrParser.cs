using System;
using System.Collections.Generic;
using System.IO;

namespace LGCallRecorder
{
    internal static class SlkCdrParser
    {
        public static CallRecord Parse(string line)
        {
            CallRecord record = new CallRecord();
            record.StationNumber = int.Parse(line.Substring(0, 5));
            record.CO = line.Substring(6, 3).Trim();
            record.Duration = TimeSpan.Parse(line.Substring(10, 8));
            record.Start = DateTime.Parse(line.Substring(19, 14));
            record.CallType = ParseCallType(line.Substring(34, 1));
            record.Dialed = line.Substring(35, 20).Trim();
            string costSubstring = line.Substring(56, 23);
            record.Cost = string.IsNullOrWhiteSpace(costSubstring) ? 0 : decimal.Parse(costSubstring);
            record.AccountCode = line.Substring(80, 20).Trim();
            record.DisconnectCause = ParseDisconnectCause(line.Substring(101, 2), record.CallType);

            return record;
        }

        public static IEnumerable<CallRecord> Parse(FileInfo slkFile)
        {
            List<CallRecord> callRecords = new List<CallRecord>();
            using (TextReader reader = slkFile.OpenText())
            {
                string line;
                CallRecord record = new CallRecord();
                while ((line = reader.ReadLine()) != null)
                {
                    string[] arr = line.Split(';');
                    if (arr[0] != "C") continue;

                    if (arr[2] == "Y1") continue;

                    string content = arr[3].Substring(2, arr[3].Length - 3);
                    switch (arr[1])
                    {
                        case "X1":
                            record.StationNumber = int.Parse(content);
                            break;
                        case "X2":
                            record.CO = content.Trim();
                            break;
                        case "X3":
                            record.Duration = TimeSpan.Parse(content);
                            break;
                        case "X4":
                            record.Start = DateTime.Parse(content);
                            break;
                        case "X5":
                            record.CallType = ParseCallType(content);
                            break;
                        case "X6":
                            record.Dialed = content.Trim();
                            break;
                        case "X7":
                            record.Cost = string.IsNullOrWhiteSpace(content) ? 0 : decimal.Parse(content);
                            break;
                        case "X8":
                            record.AccountCode = content.Trim();
                            break;
                        case "X9":
                            record.DisconnectCause = ParseDisconnectCause(content, record.CallType);
                            callRecords.Add(record);
                            record = new CallRecord();
                            break;
                    }
                }
            }

            return callRecords;
        }

        private static DisconnectCause ParseDisconnectCause(string content, CallType calltype)
        {
            if (content.StartsWith("10"))
                return DisconnectCause.NormalClearing;
            if (content.StartsWith("01"))
                return DisconnectCause.UnAllocatedNumber;
            return DisconnectCause.Unknown;
        }

        private static CallType ParseCallType(string content)
        {
            switch (content)
            {
                case "G":
                    return CallType.Group;
                case "O":
                    return CallType.Outgoing;
                case "I":
                    return CallType.Incoming;
                case "H":
                    return CallType.Hold;
                case "R":
                    return CallType.Ring;
                case "T":
                    return CallType.Transferd;
                case "t":
                    return CallType.Transferd;
                default:
                    return CallType.Unknown;
            }
        }
    }
}