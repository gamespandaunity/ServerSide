using System.Collections.Generic;
[System.Serializable]
public class WithdrawalHistoryModel
{

    public bool status;
    public string message;
    public List<Withdrawhistory> withdrawhistory;
    public string currentPage;
    public int totalPages;
    public string perPage;
    public int total_count;



}
[System.Serializable]
public class Withdrawhistory
{
    public int coins;
    public string withdraw_amount;
    public string createdAt;
    public string withdrawalstatus;
}