using ZWave;
using ZWave.UicApplication;
using UicApplication.Operations;

namespace Zats.Services
{
    public class UicToken
    {
        public class Void : BasicToken.Void
        {
            public Void(ActionToken token)
            {
                Token = token;
            }

            public override BasicResult.Void WaitCompletedSignal()
            {
                return new BasicActionResult.Void(Token.WaitCompletedSignal());
            }
        }
        public class ExpectData : _ActionToken
        {
            public ExpectData(ActionToken token)
                : base(token)
            {
                Token = token;
            }

            public UicResult.ExpectData WaitCompletedSignal()
            {
                //UicResult.ExpectData WaitCompletedReturnVale = new UicResult.ExpectData(
                //    (ExpectDataResult)Token.WaitCompletedSignal());
                //Token.Complete();
                //return WaitCompletedReturnVale;
                return  new UicResult.ExpectData((ExpectDataResult)Token.WaitCompletedSignal());
            }
        }
        public class ListenData : _ActionToken
        {
            public ListenData(ActionToken token)
                : base(token)
            {
                Token = token;
            }

            public UicResult.ListenData WaitCompletedSignal()
            {
                //UicResult.ExpectData WaitCompletedReturnVale = new UicResult.ExpectData(
                //    (ExpectDataResult)Token.WaitCompletedSignal());
                //Token.Complete();
                //return WaitCompletedReturnVale;
                return new UicResult.ListenData((ListenDataResult)Token.WaitCompletedSignal());
            }
        }
        public class Inclusion : _ActionToken
        {
            public Inclusion(ActionToken token)
                : base(token)
            { }

            public UicResult.Inclusion WaitCompletedSignal()
            {
                return new UicResult.Inclusion((InclusionResult)Token.WaitCompletedSignal());
            }
        }
        public class Exclusion : _ActionToken
        {
            public Exclusion(ActionToken token)
                : base(token)
            { }

            public UicResult.Exclusion WaitCompletedSignal()
            {
                return new UicResult.Exclusion((ExclusionResult)Token.WaitCompletedSignal());
            }
        }
    }
}
