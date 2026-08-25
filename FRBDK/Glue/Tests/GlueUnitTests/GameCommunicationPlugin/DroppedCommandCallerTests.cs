using CompilerLibrary.ViewModels;
using GameCommunicationPlugin.GlueControl;
using GameCommunicationPlugin.GlueControl.Dtos;
using GameCommunicationPlugin.GlueControl.Managers;
using Shouldly;
using System.Collections.Generic;
using ToolsUtilities;
using Xunit;

namespace GlueUnitTests.GameCommunicationPlugin
{
    /// <summary>
    /// The three callers that read a game response used to each invent their own meaning for "succeeded
    /// but empty". These pin what each does now that the transport reports a dropped command as a failure.
    /// </summary>
    public class DroppedCommandCallerTests
    {
        #region Play/edit mode switch

        [Fact]
        public void SetEditMode_WhenTheGameCarriedItOut_ReportsNothing()
        {
            var response = new GeneralResponse<GeneralCommandResponse>
            {
                Succeeded = true,
                Data = new GeneralCommandResponse { Succeeded = true }
            };

            MainCompilerPlugin.GetSetEditModeFailureMessage(response, PlayOrEdit.Edit, isConnected: true)
                .ShouldBeNull();
        }

        /// <summary>
        /// The #2203 symptom: the game replies successfully (a well-formed, non-empty payload) but that
        /// payload itself says Succeeded=false (e.g. no current Screen to restart). The transport wrapper's
        /// own Succeeded only reflects the round-trip, not this - so this has to be read from response.Data
        /// or the game's real diagnostic message never reaches the user and edit mode silently no-ops.
        /// </summary>
        [Fact]
        public void SetEditMode_WhenTheGameRepliedButRefused_SaysSoAndRepeatsTheGamesMessage()
        {
            var response = new GeneralResponse<GeneralCommandResponse>
            {
                Succeeded = true,
                Data = new GeneralCommandResponse
                {
                    Succeeded = false,
                    Message = "The ScreenManager.CurrentScreen is null, so the screen cannot be restarted."
                }
            };

            var message = MainCompilerPlugin.GetSetEditModeFailureMessage(response, PlayOrEdit.Edit, isConnected: true);

            message.ShouldNotBeNull();
            message.ShouldContain("Edit");
            message.ShouldContain("CurrentScreen is null");
        }

        /// <summary>
        /// The symptom from the issue: a dropped command answered with an empty payload read as success,
        /// so edit mode silently did not get set and nothing was printed.
        /// </summary>
        [Fact]
        public void SetEditMode_WhenTheCommandWasDropped_SaysSoAndRepeatsTheReason()
        {
            var response = GeneralResponse<GeneralCommandResponse>.UnsuccessfulWith(
                "The game is not ready to handle commands yet");

            var message = MainCompilerPlugin.GetSetEditModeFailureMessage(response, PlayOrEdit.Edit, isConnected: true);

            message.ShouldNotBeNull();
            message.ShouldContain("Edit");
            message.ShouldContain("not ready to handle commands");
            // Sent with SendImportance.RetryOnFailure, so reaching this message means the whole retry
            // budget was exhausted, not just one unlucky attempt - worth saying so for diagnostics.
            message.ShouldContain("retried for up to");
        }

        [Fact]
        public void SetEditMode_WhenThereIsNoResponseObjectAtAll_SaysSo()
        {
            var message = MainCompilerPlugin.GetSetEditModeFailureMessage(null, PlayOrEdit.Play, isConnected: true);

            message.ShouldNotBeNull();
            message.ShouldContain("no response");
        }

        /// <summary>
        /// This branch built a message and then threw it away, so a play/edit switch against a
        /// disconnected game produced no output at all.
        /// </summary>
        [Fact]
        public void SetEditMode_WhenTheGameIsNotConnected_SaysSo()
        {
            var response = new GeneralResponse<GeneralCommandResponse>
            {
                Succeeded = true,
                Data = new GeneralCommandResponse { Succeeded = true }
            };

            var message = MainCompilerPlugin.GetSetEditModeFailureMessage(response, PlayOrEdit.Edit, isConnected: false);

            message.ShouldNotBeNull("a switch that could not reach the game must not be silent");
            message.ShouldContain("not connected");
        }

        #endregion

        #region Adding objects live

        [Fact]
        public void AddObject_WhenEveryObjectWasCreated_DoesNotRestart()
        {
            var response = new GeneralResponse<AddObjectDtoListResponse>
            {
                Succeeded = true,
                Data = new AddObjectDtoListResponse
                {
                    Data = new List<AddObjectDtoResponse>
                    {
                        new AddObjectDtoResponse { CreationResponse = OptionallyAttemptedGeneralResponse.SuccessfulAttempt }
                    }
                }
            };

            RefreshManager.GetAddObjectRestartReason(response).ShouldBeNull();
        }

        [Fact]
        public void AddObject_WhenTheGameRefusedAnObject_Restarts()
        {
            var response = new GeneralResponse<AddObjectDtoListResponse>
            {
                Succeeded = true,
                Data = new AddObjectDtoListResponse
                {
                    Data = new List<AddObjectDtoResponse>
                    {
                        new AddObjectDtoResponse { CreationResponse = OptionallyAttemptedGeneralResponse.UnsuccessfulWith("no such type") }
                    }
                }
            };

            RefreshManager.GetAddObjectRestartReason(response).ShouldNotBeNull();
        }

        [Fact]
        public void AddObject_WhenTheGameSentNoData_Restarts()
        {
            var response = new GeneralResponse<AddObjectDtoListResponse> { Succeeded = true, Data = null };

            RefreshManager.GetAddObjectRestartReason(response).ShouldNotBeNull();
        }

        /// <summary>
        /// A restart costs the user a full rebuild and relaunch, so when it happens because the game never
        /// took the command, the reason has to say that rather than blaming the objects.
        /// </summary>
        [Fact]
        public void AddObject_WhenTheCommandWasDropped_RestartReasonNamesTheDrop()
        {
            var response = GeneralResponse<AddObjectDtoListResponse>.UnsuccessfulWith(
                "The game is not ready to handle commands yet");

            var reason = RefreshManager.GetAddObjectRestartReason(response);

            reason.ShouldNotBeNull();
            reason.ShouldContain("not ready to handle commands");
        }

        #endregion
    }
}
