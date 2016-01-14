using System.Text.RegularExpressions;
using System.Diagnostics;

namespace FlatRedBall.Glue.Authentication
{
    public static class Forum
    {
        private static bool Authenticate(string username, string password)
        {
            PostSubmitter post = new PostSubmitter();
            post.Url = "http://www.flatredball.com/frb/forum/ucp.php?mode=login&sid=9cd8b8da2649060b9d22d297f27a1dc7";

            post.PostItems.Add("autologin", "on");
            post.PostItems.Add("login", "Login");
            post.PostItems.Add("username", username);
            post.PostItems.Add("password", password);
            post.Type = PostSubmitter.PostTypeEnum.Post;
            string result = post.Post();

            string loggedinstring = string.Format("Logout \\[ {0} \\]", username);
            Regex r = new Regex(loggedinstring, RegexOptions.IgnoreCase);
            var match = r.Match(result);

            return match.Success;
        }

        private static void ShowRegistration()
        {
            Process.Start("http://www.flatredball.com/frb/forum/ucp.php?mode=register&sid=a9d6e6dcb7843f1126cc226fef02b3b5");
        }
    }
}
