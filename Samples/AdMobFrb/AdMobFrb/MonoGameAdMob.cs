// kod bygger på kod från https://github.com/dmanning23/AdMobBuddy

using Android.App;
using Android.Gms.Ads;
using Android.Gms.Ads.Initialization;
using Android.Media.TV;
using Android.Views;
using Android.Widget;
using Java.Lang;

namespace MonoGameAdMob
{
    public enum BannerPosition { BottomBanner, TopBanner };

    public interface IAdManager
    {
        public void ShowBannerAd(BannerPosition bannerPos, int margin = 0);
        public void HideBannerAd();
        public int BannerHeight();
    }


    public class AdMobAdapter : IAdManager
    {
        public Activity Activity { get; set; }
        public string BannerAdID { get; set; }

        private AdView adView;
        private RelativeLayout adLayoutContainer;

        public AdMobAdapter(Activity activity, string bannerAdID = "")
        {
            Activity = activity;
            BannerAdID = bannerAdID;

            MobileAds.Initialize(Activity, new InitializationListener(this));
        }

        public void ShowBannerAd(BannerPosition bannerPos, int margin = 0)
        {
            // Create the banner ad
            adView = new AdView(Activity)
            {
                AdUnitId = BannerAdID,
                AdSize = GetAdSize(),
            };

            // Create a relative layout
            adLayoutContainer = new RelativeLayout(Activity);
            var adViewParams = new RelativeLayout.LayoutParams(RelativeLayout.LayoutParams.WrapContent, RelativeLayout.LayoutParams.WrapContent);
            if (bannerPos == BannerPosition.BottomBanner)
            {
                adViewParams.AddRule(LayoutRules.AlignParentBottom, 1);
                adViewParams.BottomMargin = margin;
            }
            if (bannerPos == BannerPosition.TopBanner)
            {
                adViewParams.AddRule(LayoutRules.AlignParentTop, 1);
                adViewParams.TopMargin = margin;
            }

            // Add the banner ad to the layout
            adLayoutContainer.AddView(adView, adViewParams);

            var rootView = Activity.Window.DecorView.RootView;
            var viewGroup = rootView as ViewGroup;
            var layoutParams = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent);
            viewGroup.AddView(adLayoutContainer, layoutParams);

            // Create the ad request
            var request = CreateBuilder().Build();

            // Load the ad into the banner
            adView.LoadAd(request);
        }

        public void HideBannerAd()
        {
            if (null != adLayoutContainer && null != adView)
            {
                adLayoutContainer.RemoveView(adView);
                var rootView = Activity.Window.DecorView.RootView;
                var viewGroup = rootView as ViewGroup;
                viewGroup.RemoveView(adLayoutContainer);
            }
            adLayoutContainer = null;
            adView = null;
        }

        private AdSize GetAdSize()
        {
            // Determine the screen width to use for the ad width.
            int widthPixels = Activity.Resources.DisplayMetrics.WidthPixels;
            float density = Activity.Resources.DisplayMetrics.Density;

            int adWidth = (int)(widthPixels / density);

            // Get adaptive ad size and return for setting on the ad view.
            return AdSize.GetCurrentOrientationAnchoredAdaptiveBannerAdSize(Activity, adWidth);
        }

        private AdRequest.Builder CreateBuilder()
        {
            var builder = new AdRequest.Builder();
            return builder;
        }

        public int BannerHeight()
        {
            if (adView != null)
            {
                return adView.Height;
            }
            else
            {
                return 0;
            }
        }
    }


    public class InitializationListener : Object, IOnInitializationCompleteListener
    {
        private AdMobAdapter Adapter { get; set; }

        public InitializationListener(AdMobAdapter adpater)
        {
            Adapter = adpater;
        }

        public void OnInitializationComplete(IInitializationStatus p0)
        {

        }

    }



}