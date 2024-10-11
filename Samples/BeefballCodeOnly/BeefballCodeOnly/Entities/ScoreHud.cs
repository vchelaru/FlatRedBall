using FlatRedBall.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeefballCodeOnly.Entities;

class ScoreHud
{
    Text Team1Score;
    Text Team2Score;
    Text Team1ScoreLabel;
    Text Team2ScoreLabel;

    public int Score1
    {
        get => int.Parse(Team1Score.DisplayText);
        set => Team1Score.DisplayText = value.ToString();
    }

    public int Score2
    {
        get => int.Parse(Team2Score.DisplayText);
        set => Team2Score.DisplayText = value.ToString();
    }

    public ScoreHud()
    {
        Team1Score = new Text();
        Team1Score.DisplayText = "99";
        Team1Score.X = -150;
        Team1Score.Y = 270;
        TextManager.AddText(Team1Score);
        Team1Score.SetPixelPerfectScale();

        Team2Score = new Text();
        Team2Score.DisplayText = "99";
        Team2Score.X = 180;
        Team2Score.Y = 270;
        TextManager.AddText(Team2Score);
        Team2Score.SetPixelPerfectScale();

        Team1ScoreLabel = new Text();
        Team1ScoreLabel.DisplayText = "Team 1:";
        Team1ScoreLabel.X = -205;
        Team1ScoreLabel.Y = 270;
        TextManager.AddText(Team1ScoreLabel);
        Team1ScoreLabel.SetPixelPerfectScale();


        Team2ScoreLabel = new Text();
        Team2ScoreLabel.DisplayText = "Team 2:";
        Team2ScoreLabel.X = 124;
        Team2ScoreLabel.Y = 270;
        TextManager.AddText(Team2ScoreLabel);
        Team2ScoreLabel.SetPixelPerfectScale();

    }

    public void Destroy()
    {
        TextManager.RemoveText(Team1Score);
        TextManager.RemoveText(Team2Score);
        TextManager.RemoveText(Team1ScoreLabel);
        TextManager.RemoveText(Team2ScoreLabel);
    }
}
