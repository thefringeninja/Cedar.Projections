namespace Cedar.Projections.Example.RavenDb
{
    using System;

    internal class InventoryItemView
    {
        public Guid Id { get; set; }
        public string Sku { get; set; }
        public int Quantity { get; set; }

        public InventoryItemView(Guid id)
        {
            Id = id;
        }
    }
}