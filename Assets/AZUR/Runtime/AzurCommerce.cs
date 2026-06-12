namespace AZUR
{
    public static class AzurCommerce
    {
        public static void TrackPurchase(
            string productId,
            string currency,
            double revenue,
            string transactionId = "",
            int quantity = 1,
            bool isSubscription = false)
        {
            AzurSdk.TrackPurchase(new AzurPurchaseEvent(
                productId,
                currency,
                revenue,
                transactionId,
                quantity,
                isSubscription));
        }
    }
}
