using System;
using System.Collections.Generic;
using System.Text;

using FlatRedBall.Math;

namespace FlatRedBall.Graphics.Particle
{
    /// <summary>
    /// List of Emitters provoding shortcut methods for interacting with all contained Emittes.
    /// </summary>
    /// <remarks>
    /// This is the runtime object created when loading .emix files.
    /// </remarks>
    [Obsolete]
    public class EmitterList : PositionedObjectList<Emitter>, IEquatable<EmitterList>
    {
        #region Methods

        public void AddToManagers()
        {
            foreach (Emitter emitter in this)
            {
                SpriteManager.AddEmitter(emitter);
            }
        }

        public void AddToManagers(Layer layer)
        {
            int count = this.Count;
            for(int i = 0; i < count; i++)
            {
                Emitter emitter = this[i];
                SpriteManager.AddEmitter(emitter, layer);
            }

        }

        public EmitterList Clone()
        {
            EmitterList newList = new EmitterList();

            foreach (Emitter emitter in this)
            {
                newList.Add(emitter.Clone());
            }

            return newList;
        }

        public void Emit()
        {
            int count = Count;

            for (int i = 0; i < count; i++)
            {
                this[i].Emit();
            }
        }

        public void Emit(SpriteList spriteList)
        {
            int count = Count;

            for (int i = 0; i < count; i++)
            {
                this[i].Emit(spriteList);
            }
        }

        public void ForceUpdateDependencies()
        {
            for (int i = 0; i < this.Count; i++)
            {
                Emitter emitter = this[i];

                emitter.ForceUpdateDependencies();
            }
        }

		public void RemoveFromManagers()
		{
            RemoveFromManagers(true);

		}

        public void RemoveFromManagers(bool clearThis)
        {
            if (!clearThis)
            {
                this.MakeOneWay();
            }

			// reverse list this
			for (int i = this.Count - 1; i > -1; i--)
			{
                Emitter emitter = this[i];

                PositionedObject oldParent = emitter.Parent;

				SpriteManager.RemoveEmitter(this[i]);

                if (!clearThis && oldParent != null)
                {
                    emitter.AttachTo(oldParent, false);
                }
			}

            if (clearThis)
            {
                Clear();
            }
            else
            {
                MakeTwoWay();

            }


        }

		public void TimedEmit()
        {
            int count = Count;

            for (int i = 0; i < count; i++)
            {
                this[i].TimedEmit();
            }
        }

        public void UpdateDependencies(double currentTime)
        {
            for (int i = 0; i < this.Count; i++)
            {
                Emitter emitter = this[i];

                emitter.UpdateDependencies(currentTime);
            }
        }

        #endregion


        #region IEquatable<EmitterList> Members

        bool IEquatable<EmitterList>.Equals(EmitterList other)
        {
            return this == other;
        }

        #endregion
    }
}
