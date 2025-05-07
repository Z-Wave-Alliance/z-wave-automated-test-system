using System;
using System.Collections.Generic;
using System.Linq;

namespace Zats.Services
{
    public class Iter
    {
        public int TxRTriesCount { get; set; }
        public double TxRTriesRate { get; set; }

        public Dictionary<TransmitStatuses, int> TxCount = new Dictionary<TransmitStatuses, int>();
        public int TxErrorCount = 0;
        public int TxTotalCount = 0;

        List<int> realArr = new List<int>();
        List<int> retryArr = new List<int>();
        List<int> sendArr = new List<int>();

        public Dictionary<int, int> DeltaTicksHistogram { get; set; }
        public Dictionary<int, int> RetryHistogram { get; set; }

        private bool _is700;
        public Iter(bool is700)
        {
            _is700 = is700;
            DeltaTicksHistogram = new Dictionary<int, int>();
            RetryHistogram = new Dictionary<int, int>();
        }

        private void OnAny(BasicResult.SendData sendRes)
        {
            realArr.Add(sendRes.ElapsedMs / 10);
            sendArr.Add(sendRes.TransmitTicks);
            retryArr.Add(sendRes.RouteTries);
            if (!TxCount.ContainsKey(sendRes.TransmitStatus))
            {
                TxCount.Add(sendRes.TransmitStatus, 0);
            }
            TxCount[sendRes.TransmitStatus]++;
            if (sendRes.TransmitStatus == TransmitStatuses.CompleteOk)
            {
                var rt = (byte)(_is700 ? sendRes.RouteTries : (sendRes.RouteTries < 4 ? 1 : sendRes.RouteTries - 3));
                TxRTriesCount += rt;
                if (TxCount.ContainsKey(TransmitStatuses.CompleteOk))
                {
                    TxRTriesRate = 100 * ((double)(TxRTriesCount - TxCount[TransmitStatuses.CompleteOk])) / TxCount[TransmitStatuses.CompleteOk];
                }
            }
        }

        public void Parse(BasicResult.SendData sendRes)
        {
            TxTotalCount++;
            if (sendRes)
            {
                OnAny(sendRes);
            }
            else
            {
                TxErrorCount++;
            }
        }

        public int RealTmean { get; set; }
        public int SendTmean { get; set; }
        int RealTmean90p;
        int SendTmean90p;
        string RealStrArr;
        string SendStrArr;
        public void Report()
        {
            RealTmean = (int)realArr.Average();
            SendTmean = (int)sendArr.Average();
            RealTmean90p = (int)realArr.Average();
            SendTmean90p = (int)sendArr.Average();
            int count = sendArr.Count;
            int p5 = (count / (100 / 5));
            if (count > (p5 * 2))
            {
                try
                {
                    RealTmean90p = (int)realArr.OrderBy(x => x).Skip(p5).Take(count - (p5 * 2)).Average();
                    SendTmean90p = (int)sendArr.OrderBy(x => x).Skip(p5).Take(count - (p5 * 2)).Average();
                }
                catch { }
            }
            RealStrArr = realArr.OrderBy(x => x).Select(x => x.ToString()).Aggregate((a, b) => $"{a},{b}").ToString();
            SendStrArr = sendArr.OrderBy(x => x).Select(x => x.ToString()).Aggregate((a, b) => $"{a},{b}").ToString();

            BuildHistogram(DeltaTicksHistogram, realArr, 40);
            BuildHistogram(RetryHistogram, retryArr, 10);
        }

        private static void BuildHistogram(Dictionary<int, int> histogram, IEnumerable<int> array, int buckets)
        {
            var maxDeltaTicks = array.OrderByDescending(x => x).First();
            var bucketSizeDeltaTicks = (int)Math.Max(Math.Floor((double)array.OrderByDescending(x => x).First() / buckets), 1);
            for (int i = 0; i <= buckets * 2; i++)
            {
                var key = i * bucketSizeDeltaTicks;

                if (!histogram.ContainsKey(key))
                {
                    histogram.Add(key, 0);
                }
                if (key > maxDeltaTicks)
                {
                    break;
                }
            }
            foreach (var item in array.OrderBy(x => x))
            {
                int bucketIndex = item;
                while (!histogram.ContainsKey(bucketIndex))
                {
                    bucketIndex--;
                }
                histogram[bucketIndex]++;
            }
        }
    }
}
