using BepInEx;

namespace PalaCompany
{
    [BepInPlugin("miskin213.pala_company", "Pala Company", "1.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        private void Awake()
        {
            Logger.LogInfo("Pala Company");
        }
    }
}