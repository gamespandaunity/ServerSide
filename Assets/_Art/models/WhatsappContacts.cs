using System.Collections.Generic;
[System.Serializable]
public class WhatsappContacts
{
    public bool status;
    public string message;
    public Data data;
    [System.Serializable]
    public class Data
    {
        public string _id;
        public string status;
        public List<string> country;
        public List<string> phone;
        public string remarks;

    }

}


