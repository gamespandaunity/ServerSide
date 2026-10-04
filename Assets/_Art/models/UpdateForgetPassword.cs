[System.Serializable]
public class UpdateForgetPassword
{
    public string phone_no;
    public string new_password;

   public UpdateForgetPassword(string phoneNumber, string new_password)
    {
        this.phone_no = phoneNumber;
        this.new_password = new_password;
    }
}
