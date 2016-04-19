namespace NewProjectCreator.ViewModels
{
    public  class TemplateViewModel
    {
        public string Name
        {
            get
            {
                return BackingData.FriendlyName;
            }
        }

        public PlatformProjectInfo BackingData
        {
            get;
            set;
        }

    }
}