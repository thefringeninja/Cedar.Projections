namespace Cedar.Projections.Example.RavenDb
{
    using System;
    using System.Runtime.Serialization;

    [DataContract]
    internal class InventoryCheckedOut
    {
        [DataMember]
        public readonly int Quantity;
        [DataMember]
        public readonly Guid Id;
        [DataMember]
        public readonly string Sku;

        public InventoryCheckedOut(Guid id, string sku, int quantity)
        {
            Id = id;
            Sku = sku;
            Quantity = quantity;
        }
    }
}