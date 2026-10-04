using NetworkManagement;

public class AightBallPoolProductsUIManager : ProductsUIManager
{
    void Awake()
    {
        if (!EightBallPoolNetworkManager.initialized)
        {
            return;
        }
        ShowGroupUI(AightBallPoolNetworkGameAdapter.is3DGraphics ? 0 : 1);
    }
}