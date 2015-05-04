namespace Cedar.Projections.Example.RavenDb
{
    using System;
    using System.Runtime.Serialization;

    [DataContract]
    internal class InventoryCheckedIn
    {
        [DataMember] public readonly int Quantity;
        [DataMember]
        public readonly Guid Id;
        [DataMember]
        public readonly string Sku;

        public InventoryCheckedIn(Guid id, string sku, int quantity)
        {
            Id = id;
            Sku = sku;
            Quantity = quantity;
        }
    }
}