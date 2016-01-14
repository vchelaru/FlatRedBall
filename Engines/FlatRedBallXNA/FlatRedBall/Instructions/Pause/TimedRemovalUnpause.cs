using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Graphics.Particle;

namespace FlatRedBall.Instructions.Pause
{
    /// <summary>
    /// Used to pause the timed removal list in the SpriteManager
    /// </summary>
    public class TimedRemovalUnpause : Instruction
    {
        double mPauseTime;
        List< TimedRemovalRecord > mTimedRemovalRecordList;

        #region Properties
        public override object Target
        {
            get { return null; }
            set { throw new InvalidOperationException(); }
        }
        #endregion

        #region Constructor
        public TimedRemovalUnpause( List< TimedRemovalRecord > mCurrentTimedRemoval )
        {
            mTimedRemovalRecordList = mCurrentTimedRemoval;
            mPauseTime = TimeManager.CurrentTime;
        }
        #endregion

        #region Methods
        public override void Execute()
        {
            double amountToAdvance = TimeManager.CurrentTime - mPauseTime;
            for( int iCurRecord = 0; iCurRecord < mTimedRemovalRecordList.Count; ++iCurRecord )
            {
                TimedRemovalRecord trr = mTimedRemovalRecordList[iCurRecord];
                trr.TimeToRemove += amountToAdvance;
                mTimedRemovalRecordList[iCurRecord] = trr;
            }
            // August 28, 2012
            // We used to replace
            // the mTimedRemovalList
            // with this.mTimedRemovalRecordList
            // but we no longer do that because there
            // could have been emissions while the engine
            // was paused.  If so, we don't want to wipe out
            // the removals.  We will do an AddRange instead.
            //SpriteManager.mTimedRemovalList = mTimedRemovalRecordList;
            foreach (var item in mTimedRemovalRecordList)
            {
                SpriteManager.mTimedRemovalList.InsertSorted(item);
            }
        }

        public override void ExecuteOn(object target)
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}
