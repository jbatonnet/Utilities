using System;
using System.Reflection;

namespace Utilities.Remoting
{
    public abstract class RemotingAccessPolicy
    {
        private class BasicRemotingAccessPolicy : RemotingAccessPolicy
        {
            private bool allowed;

            public BasicRemotingAccessPolicy(bool allowed)
            {
                this.allowed = allowed;
            }

            public override RemotingAccessPolicy GetAccessPolicy(MemberInfo method)
            {
                if (!allowed)
                    throw new AccessViolationException();

                return allowed ? Allowed : Denied;
            }
        }

        public static RemotingAccessPolicy Allowed { get; } = new BasicRemotingAccessPolicy(true);
        public static RemotingAccessPolicy Denied { get; } = new BasicRemotingAccessPolicy(false);

        public abstract RemotingAccessPolicy GetAccessPolicy(MemberInfo member);
    }
}
