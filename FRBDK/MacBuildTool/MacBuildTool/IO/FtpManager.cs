using System;
using System.Net;
using System.IO;
using System.Threading;
using System.Text;

namespace MacBuildTool
{
	
	public class FtpState
	{
		private ManualResetEvent wait;
		private string fileName;
		private Exception operationException = null;
		string status;
		
		private FtpWebRequest request;
		
		public FtpState()
		{
			wait = new ManualResetEvent(false);
		}
		
		public ManualResetEvent OperationComplete
		{
			get { return wait; }
		}
		
		public FtpWebRequest Request
		{
			get { return request; }
			set { request = value; }
		}
		
		public string FileName
		{
			get { return fileName; }
			set { fileName = value; }
		}
		
		public string TextToUpload
		{
			get;
			set;
		}
		
		public Exception OperationException
		{
			get { return operationException; }
			set { operationException = value; }
		}
		public string StatusDescription
		{
			get { return status; }
			set { status = value; }
		}
		
		
	}





	public static class FtpManager
	{
		private static FtpWebRequest GetFtpWebRequest(string url, string userName, string password, string method, bool keepAlive)
		{
			return GetFtpWebRequest(url, userName, password, method, keepAlive, false);
		}
		private static FtpWebRequest GetFtpWebRequest(string url, string userName, string password, string method, bool keepAlive, bool forceBinary)
		{
			if (!url.StartsWith("ftp://"))
			{
				throw new ArgumentException("The URL \n" + url + "\ndoes not begin with \n\"ftp://\"  \nThis is a requirement");
			}
			
			Uri uri = new Uri(url, UriKind.Absolute);
			
			
			FtpWebRequest ftpclientRequest = WebRequest.Create(uri) as FtpWebRequest;
			
			ftpclientRequest.KeepAlive = keepAlive;
			
			ftpclientRequest.Method = method;
			switch (method)
			{
				case WebRequestMethods.Ftp.ListDirectoryDetails:
					ftpclientRequest.Proxy = null;
					break;
				case WebRequestMethods.Ftp.DownloadFile:
					ftpclientRequest.UseBinary = forceBinary;
					break;
				case WebRequestMethods.Ftp.UploadFile:
					ftpclientRequest.UsePassive = true;
					
					ftpclientRequest.UseBinary = true;
					break;
			}
			
			
			ftpclientRequest.Credentials = new NetworkCredential(userName, password);
			
			return ftpclientRequest;
		}

		public static void UploadFile(string localFileToUpload, string targetUrl, string userName, string password, bool keepAlive)
		{
			
			FtpState state = new FtpState();
			
			state.FileName = localFileToUpload;
			
			
			StartUploadUsingFtpState(targetUrl, userName, password, keepAlive, state);
		}

		public static void SaveFile(string url, string localFile, string userName, string password)
		{
			ThrowExceptionIfFtpUrlIsBad(url);
			Uri uri = new Uri(url, UriKind.Absolute);
			
			FtpWebRequest request = GetFtpWebRequest(
				url, userName, password, WebRequestMethods.Ftp.DownloadFile, false, true);
			
			FtpWebResponse response = (FtpWebResponse)request.GetResponse();
			Stream ftpStream = response.GetResponseStream();
			
			string directoryToCreate = FileManager.GetDirectory(localFile, RelativeType.Absolute);
			if (!string.IsNullOrEmpty(directoryToCreate))
			{
				Directory.CreateDirectory(directoryToCreate);
			}
			
			if (File.Exists(localFile))
			{
				File.Delete(localFile);
			}
			
			FileStream fileStream = File.OpenWrite(localFile);
			
			
			
			byte[] buffer = new byte[200000];
			
			int amountRead = 0;
			
			while ((amountRead = ftpStream.Read(buffer, 0, buffer.Length)) != 0)
			{
				fileStream.Write(buffer, 0, amountRead);
			}
			
			fileStream.Close();
			ftpStream.Close();
			response.Close();
		}

		private static void ThrowExceptionIfFtpUrlIsBad(string url)
		{
			if (!url.StartsWith("ftp://"))
			{
				throw new ArgumentException("The URL \n" + url + "\ndoes not begin with \n\"ftp://\"  \nThis is a requirement");
			}
		}
	
		
		public static bool IsFtp(string url)
		{
			return url.StartsWith("ftp://");
		}


		private static void StartUploadUsingFtpState(string targetUrl, string userName, string password, bool keepAlive, FtpState state)
		{
			ManualResetEvent waitObject;
			
			FtpWebRequest request;
			
			bool fileUploaded = false;
			do
			{
				try
				{

					
					request = GetFtpWebRequest(targetUrl, userName, password, WebRequestMethods.Ftp.UploadFile, keepAlive);



					waitObject = state.OperationComplete;
					state.Request = request;
					
					var result = request.BeginGetRequestStream(
						new AsyncCallback(EndGetStreamCallback),
						state
						);

					waitObject.WaitOne();


					var resultAsyncState = (FtpState)result.AsyncState;
					if(resultAsyncState.OperationException != null)
					{
						throw resultAsyncState.OperationException;
					}


					fileUploaded = true;
//					state.Request.GetResponse();

				}
				catch (WebException e)
				{
					if (e.ToString().Contains("550"))
					{
						fileUploaded = false;
//						CreateDirectory(userName, password, ReturnBaseDirectory(targetUrl), 2);
					}
					if(e.ToString ().Contains ("530"))
					{
						throw e;

					}
					else
					{
						throw e;
					}
				}
				catch(Exception exception)
				{
					int m = 3;
				}
			} while (!fileUploaded);
		}

		
		private static void EndGetStreamCallback(IAsyncResult result)
		{
			FtpState state = (FtpState)result.AsyncState;
			
			Stream requestStream = null;
			
			Stream uploadStream = null;
			
			try
			{
				requestStream = state.Request.EndGetRequestStream(result);
				const int bufferLength = 2048;
				byte[] buffer = new byte[bufferLength];
				int count = 0;
				int readBytes = 0;
				
				if (!string.IsNullOrEmpty(state.TextToUpload))
				{
					byte[] byteArray = Encoding.ASCII.GetBytes(state.TextToUpload);
					uploadStream = new MemoryStream(byteArray);
				}
				else
				{
					uploadStream = File.OpenRead(state.FileName);
				}
				
				do
				{
					readBytes = uploadStream.Read(buffer, 0, bufferLength);
					requestStream.Write(buffer, 0, readBytes);
					count += readBytes;
				}
				while (readBytes != 0);
				
				requestStream.Close();
				
				state.Request.BeginGetResponse(
					new AsyncCallback(EndGetResponseCallback),
					state
					);
			}
			catch (WebException webException)
			{
				state.Request.Abort();
				state.OperationComplete.Set();
				state.OperationException = webException;
			}
			finally
			{
				if (uploadStream != null)
				{
					uploadStream.Close();
				}
			}
		}

		
		private static void EndGetResponseCallback(IAsyncResult result)
		{
			FtpState state = (FtpState)result.AsyncState;
			FtpWebResponse response = null;
			response = (FtpWebResponse)state.Request.EndGetResponse(result);
			response.Close();
			state.StatusDescription = response.StatusDescription;
			
			state.OperationComplete.Set();
		}
	}
}

