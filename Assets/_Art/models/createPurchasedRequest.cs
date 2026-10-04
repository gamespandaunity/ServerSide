[System.Serializable]
public class createPurchasedRequest
{
    public bool status;
    public string message;
    public Purchase purchase;
    [System.Serializable]
    public class Purchase
    {
        public string amount;
        public string transaction_id;
        public string remarks;
        public string gold_coin;
        public string silver_coin;
        public string silver_coin_amount;
        public string status;
        public string user_id;
        public string country;
        public string first_name;
        public string last_name;
        public string userId;
        public string purchase_id;
        public string _id;

    }
}
