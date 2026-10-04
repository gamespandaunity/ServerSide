public class LoginModel
{

    public string phone;
    public string password;
    public string auth_identifier;
 

    public LoginModel(string phoneNumber, string password,string auth_identifier)
    {
        this.phone = phoneNumber;
        this.password = password;
        this.auth_identifier = auth_identifier;
    }
}


