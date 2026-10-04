[System.Serializable]
public class walletModel
{

    public bool status;
    public Wallet? wallet;
    public string? message;

}

[System.Serializable]
public class Wallet
{
    public string? wallet_no;
    public string? wallet_name;
    public string? user_id;
    public int? is_selected;
    public string? _id;
    public string? createdAt;
    public string? updatedAt;
}
[System.Serializable]
public class CreateWallet
{
    public string? wallet_no;
    public string? wallet_name;
    public string? user_id;
}
[System.Serializable]
public class UpdateWallet
{
    public string? wallet_no;
    public string? wallet_name;
    public string? user_id;
    public int? is_selected;
}