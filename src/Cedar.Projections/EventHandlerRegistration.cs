namespace Cedar.Projections
{
    using System;

    internal class EventHandlerRegistration
    {
        private readonly object _handlerInstance;
        private readonly Type _registrationType;

        internal EventHandlerRegistration(Type registrationType, object handlerInstance)
        {
            _registrationType = registrationType;
            _handlerInstance = handlerInstance;
        }

        public Type RegistrationType
        {
            get { return _registrationType; }
        }

        public object HandlerInstance
        {
            get { return _handlerInstance; }
        }
    }
}