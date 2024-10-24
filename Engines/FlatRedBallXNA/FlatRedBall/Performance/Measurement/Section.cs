using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using FlatRedBall.IO;
using System.IO;
//using System.IO.Compression;

namespace FlatRedBall.Performance.Measurement
{
    public class Section : IDisposable
    {
        #region Fields

        double mTimeStarted;
        bool mStarted;

        static Section mContext;

        #endregion

        #region Properties

        [XmlIgnore]
        public Section Parent
        {
            get;
            private set;
        }

        [XmlIgnore]
        public Section TopParent
        {
            get
            {
                if (Parent == null)
                {
                    return this;
                }
                else
                {
                    return Parent.TopParent;
                }
            }
        }

        [XmlElement("C")]
        public List<Section> Children
        {
            get;
            set;
        }

        [XmlAttribute("T")]
        public float Time
        {
            get;
            set;
        }

        [XmlAttribute("N")]
        public string Name
        {
            get;
            set;
        }

        [XmlIgnore]
        public bool IsCurrentContext
        {
            get
            {
                return this == mContext;
            }
        }

        public static Section Context
        {
            get { return mContext; }
        }

        public static int ContextDepth
        {
            get
            {
                int toReturn = 0;
                Section context = Context;
                while (context != null)
                {
                    toReturn++;
                    context = context.Parent;
                }
                return toReturn;
            }
        }
        #endregion

        public Section()
        {
            Children = new List<Section>();

            if (mContext != null)
            {
                mContext.Children.Add(this);
                this.Parent = mContext;


            }
        }

        //public static Section GetAndStartContext(string name)
        //{
        //    Section section = new Section();
        //    section.Name = name;
        //    section.StartContext();
        //    return section;
        //}

        //public static Section GetAndStartTime(string name)
        //{

        //    Section section = new Section();
        //    section.Name = name;
        //    section.StartTime();
        //    return section;

        //}

        public static Section GetAndStartContextAndTime(string name)
        {
            Section section = new Section();
            section.Name = name;
            section.StartContext();
            section.StartTime();
            return section;


        }

        public static Section GetAndStartMergedContextAndTime(string name)
        {
            Section section = null;
            if (mContext != null)
            {

                section = mContext.Children.FirstOrDefault(item => item.Name == name);
                if (section != null)
                {
                    section.mTimeStarted = TimeManager.SystemCurrentTime - section.Time;
                    section.mStarted = true;
                    section.StartContext();
                    // no need to do this, we do it above by manually setting the time started and whether it's started
                    //section.StartTime();
                }
                else //(section == null)
                {
                    section = GetAndStartContextAndTime(name);
                }
            }
            return section;
        }

        public static Section EndContextAndTime()
        {
            // Store this off because ending context could set a new mContext
            Section toReturn = mContext;
            mContext.EndTimeAndContext();
            return toReturn;
        }

        public void StartContext()
        {
            mContext = this;
        }

        public void StartTime()
        {
            mTimeStarted = TimeManager.SystemCurrentTime;
            mStarted = true;
        }


        public void EndContext()
        {
            if (mContext == this)
            {
                mContext = this.Parent;
            }
            else
            {
                string message = "This Section is not the context, so it can't end it.";
                if(mContext == null)
                {
                      message += "Current context is null";
                }
                else
                {
                    message += "Current context is " + mContext.Name;
                }
                throw new Exception(message);
            }
        }

        public void EndTime()
        {
            if (!mStarted)
            {
                throw new Exception("Could not end time because it hasn't been started, or it has already been ended");
            }
            Time = (float)(TimeManager.SystemCurrentTime - mTimeStarted);
            mStarted = false;
        }

        public void EndTimeAndContext()
        {
            EndTime();
            EndContext();

        }

        public void Save(string fileName)
        {
			// This doesn't use FileManager.XmlSerialize because FileManager.XmlSerialize
			// requires files to be saved to the user's folder on platforms like Android. Sections
			// are saved for diagnostic reasons so their save location should not be restricted.
			string outputText;
			FlatRedBall.IO.FileManager.XmlSerialize(typeof(FlatRedBall.Performance.Measurement.Section), this, out outputText);
			System.IO.File.WriteAllText(fileName, outputText);
        }

        public override string ToString()
        {
            return "Name: " + Name + " Time: " + Time;
        }

		public string ToStringVerbose()
		{
			return ToStringVerbose (int.MaxValue);

		}

		public string ToStringVerbose(int depth)
		{
			StringBuilder stringBuilder = new StringBuilder ();

			ToStringVerbose (stringBuilder, 0, depth);
			return stringBuilder.ToString ();
		}


		internal void ToStringVerbose(StringBuilder stringBuilder, int spaces, int depth)
		{
			if (depth > 0)
			{
				if (Children.Count == 0 || depth == 1)
				{
					stringBuilder.AppendLine (new string (' ', spaces) + "<" + ToString () + ">");
				} else
				{
					stringBuilder.AppendLine (new string (' ', spaces) + "<" + ToString ());

					foreach (var item in Children)
					{
						item.ToStringVerbose (stringBuilder, spaces + 2, depth-1);
					}
					stringBuilder.AppendLine (new string (' ', spaces) + ">");
				}
			}
		}


        public string GetAsXml()
        {
            string body;

            FileManager.XmlSerialize(this, out body);

            return body;

        }
           

        public void SendToEmail(string address)
        {
            string body;
            
            FileManager.XmlSerialize(this, out body);


            string compressed = Compress(body);


            FileManager.SaveText(compressed, "Compressed.txt");

        }

        private static string Compress(string body)
        {
            // Vic July 9, 2018
            // I think this was commented out because I never got it to work, but I just figured
            // out the problem. I was writing to the GZipStream using the body's length instead of
            // the # of bytes being written. I added the fix but kept the code commented out in case
            // it's commented out for a reason...but I do think it will work now

            //MemoryStream memoryStream = new MemoryStream();
            //GZipStream zipStream = new GZipStream(memoryStream, CompressionMode.Compress);
            // var bytesToWrite = UTF8Encoding.UTF8.GetBytes(body);
            //zipStream.Write(bytesToWrite, 0, bytesToWrite.Length);

            //string compressed = Convert.ToBase64String(memoryStream.ToArray());
            //return compressed;
            return body;
        }


        public static Section FromBase64GzipFile(string fileName)
        {
            string text = FileManager.FromFileText(fileName);
            string decompressedString = Decompress(text);

            return FileManager.XmlDeserializeFromString<Section>(decompressedString);
        }

        private static string Decompress(string text)
        {
            //byte[] encodedDataAsBytes = Convert.FromBase64String(text);

            //MemoryStream memoryStream = new MemoryStream(encodedDataAsBytes);
            //GZipStream zipStream = new GZipStream(memoryStream, CompressionMode.Decompress);
            //MemoryStream decompressedStream = new MemoryStream();
            //var buffer = new byte[4096];
            //int read;

            //while ((read = zipStream.Read(buffer, 0, buffer.Length)) > 0)
            //{
            //    decompressedStream.Write(buffer, 0, read);
            //}

            //string decompressedString = UTF8Encoding.UTF8.GetString(decompressedStream.ToArray());
            //return decompressedString;
            return text;
        }

        public void SetParentRelationships()
        {
            foreach (var child in Children)
            {
                child.Parent = this;
                child.SetParentRelationships();
            }
        }


        public void Dispose()
        {
            EndTimeAndContext();
        }
    }
}
