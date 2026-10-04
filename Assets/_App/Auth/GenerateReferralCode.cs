[System.Serializable]
public class GenerateReferralCode 
{

    public bool status;
    public Referal referal;
  

}
public class Referal
{
    public string _id;
    public string referral_code;
    public string status;
    public string user_id;
    public string total_use;
    public string use_date;
    public string createdAt;
    public string updatedAt;

}
