using NekkoYa.Editor;
namespace NekkoYa.AssetOrganizer.Editor
{
    internal static class OrganizerLocalization
    {
        public static string Text(string english, string japanese)
        {
            return NekkoYaLocalization.Language == NekkoYaLanguage.EN ? english : japanese;
        }
    }
}
