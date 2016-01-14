using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;



namespace FlatRedBallAddOns.Screens
{
    public class ScreenTemplate : Screen
    {
        #region Methods

        #region Constructor and Initialize

        public ScreenTemplate() : base("ScreenTemplate")
        {
            // Don't put initialization code here, do it in
            // the Initialize method below
            //   |   |   |   |   |   |   |   |   |   |   |
            //   |   |   |   |   |   |   |   |   |   |   |
            //   V   V   V   V   V   V   V   V   V   V   V

        }

        public override void Initialize()
        {
            // Set the screen up here instead of in the Constructor to avoid
            // exceptions occurring during the constructor.
            base.Initialize();
        }

        #endregion

        #region Public Methods

        public override void Activity(bool firstTimeCalled)
        {
            base.Activity(firstTimeCalled);
        }

        public override void Destroy()
        {
            base.Destroy();
        }

        #endregion

        #endregion
    }
}
