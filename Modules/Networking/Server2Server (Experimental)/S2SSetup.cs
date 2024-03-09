using System;

namespace Cutulu
{
    using Client = ClientNetwork<S2SSetup>;
    public partial class S2SSetup : Receiver
    {
        private Action<Client, Passkey> callback;

        public S2SSetup(Action<Client, Passkey> callback)
        {
            this.callback = callback;
        }

        public override void Receive(byte key, byte[] bytes, Method method, params object[] values)
        {
            if (key != 187 || values.IsEmpty() || values[0] is not Client client) return;

            callback?.Invoke(client, new(bytes));
        }
    }
}