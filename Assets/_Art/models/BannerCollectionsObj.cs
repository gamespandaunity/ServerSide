using System.Collections.Generic;

[System.Serializable]
public class BannerCollectionsObj
{
    
    public bool status;

    public string message;

    public Countries countries;
}

[System.Serializable]
public class BannerList
{
    public string _id;
    public string status;
    public string title;
    public string description;
    public string file_url;
    public string createdAt;
    public string updatedAt;

}
[System.Serializable]
public class Countries
{
    public string _id;
    public string status;
    public string collection_name;
    public List<string> banner_id;
    public string created_by;
    public List<BannerList> banner_list;
}








