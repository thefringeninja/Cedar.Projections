namespace Cedar.Projections
{
    public delegate void EventHandlerSync<TMessage>(EventMessage<TMessage> eventMessage) 
        where TMessage : class;
}