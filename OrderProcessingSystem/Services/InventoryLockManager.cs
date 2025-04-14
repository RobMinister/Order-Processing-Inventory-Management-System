namespace OrderProcessingSystem.Services
{
    public static class InventoryLockManager
    {
        private static readonly Dictionary<int, object> _locks = new();
        private static readonly object _globalLock = new();

        public static object GetLockForProduct(int productId)
        {
            lock (_globalLock)
            {
                if (!_locks.ContainsKey(productId))
                {
                    _locks[productId] = new object();
                }

                return _locks[productId];
            }
        }
    }
}
