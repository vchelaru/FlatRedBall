## Introduction

Thanks for your interest in contributing to FlatRedBall. The FlatRedBall Engine and associated tool are continually-evolving, and your involvement can help move the technology forward even faster. There's a lot of ways to get involved.

## Discord Chat

The FlatRedBall Discord server is an active place to ask and answer questions. The more activity in the Discord server, the more confident new developers will feel when evaluating FlatRedBall. Joining the server and chatting can help the community grow and stay active. 

FlatRedBall Discord: https://discord.gg/tG5RBgw

## Reporting a Bug

If you believe you have encountered a bug with the FlatRedBall Engine or tools, please report a Github Issue. When reporting a bug, please include the minimum steps requires to reproduce the problem. Include very detailed steps and end the steps with the **observed** problem and **expected** behavior. If there are workarounds or actions which change the behavior of the bugs, include that as additional information after the steps. See below for an example:

1. Open a new project in the FlatRedBall Editor (Desktop GL)
1. Run the Wizard, and select the platformer game type
2. Wait for the wizard to finish
3. Create a new Entity named Monster
4. Add an instance of the Monster Entity to the GameScreen
5. Click the Variables tab

**Observe**: The Z value is missing from the Monster entity instance

**Expected**: The Z value should be available on the Monster entity instance

**Additional Information**: Restarting Glue fixes the problem - the variable shows after a restart.

If possible, include:

* Screenshots of the problem
* Callstacks of the errors (many errors in Glue or in the FlatRedBall Engine will provide them)
* Entire projects if the steps to reproduce are complicated or unknown. If including a project, please exclude the bin and obj folders as they are unnecessary and can use a lot of extra space

## Asking Questions

Questions can be asked on Discord (as mentioned above) or as an issue on Github. When asking questions on github, please include information about what you are attempting to accomplish, what you've tried, and how the outcome differs from what you expect. Following the same guidelines as **Reporting a Bug** is helpful.

## Suggestions for Improvements

If you have an idea for how to improve FlatRedBall, please let us know in the Discord channel. We believe that FlatRedBall has an unlimited amount of potential to improve game development, and often times we have certain features planned, but they haven't yet been worked on due to implementation difficulty. We prefer to keep discussions about potential improvements in Discord so that we can talk about the details of why a particular feature may or may not make sense given the current state of development.

## Making Code Contributions

If you have code which you would like to submit to FlatRedBall, please follow the standard fork/pull request approach. At the time of this writing we have not been adding issues labeled specifically as **Help Wanted** but this may change over time.

If you would like to contribute, but you don't know where to help, join the Discord chat and discuss ideas with other contributors.
