using HalconDotNet;

using OpenCvSharp;

using System;
using System.Runtime.InteropServices;

namespace Convertor
{
	public class ConvertorHelper
    {
		[DllImport("kernel32.dll", EntryPoint = "RtlMoveMemory", SetLastError = false)]
		public static extern void CopyMemory(IntPtr dest, IntPtr src, uint count);

		[DllImport("kernel32.dll", EntryPoint = "RtlMoveMemory", SetLastError = false)]
		public static extern void CopyMemory(int dest, int src, int count);

		public static HImage Mat2HImage(Mat mat)
        {
			HImage hImage = new HImage();
			int height = mat.Rows;
			int width = mat.Cols;

			if (mat.Type() == MatType.CV_8UC1)
			{
				Mat[] mats = mat.Split();
				Mat mat1 = mats[0];

				IntPtr ptr1 = Marshal.AllocHGlobal(height * width);

				unsafe
				{
					for (int i = 0; i < height; i++)
					{
						IntPtr src1 = IntPtr.Add(mat1.Data, width * i);

						CopyMemory(IntPtr.Add(ptr1, width * i), src1, (uint)width);
					}
				}

				hImage.GenImage1("byte", width, height, ptr1);
			}
			else if (mat.Type() == MatType.CV_8UC3)
			{
				Mat[] mats = mat.Split();   // 0:B  1:G  2:R
				Mat matR = mats[2];
				Mat matG = mats[1];
				Mat matB = mats[0];

				IntPtr ptrR = Marshal.AllocHGlobal(height * width);
				IntPtr ptrG = Marshal.AllocHGlobal(height * width);
				IntPtr ptrB = Marshal.AllocHGlobal(height * width);

				unsafe
				{
					for (int i = 0; i < height; i++)
					{
						IntPtr srcR = IntPtr.Add(matR.Data, width * i);
						IntPtr srcG = IntPtr.Add(matG.Data, width * i);
						IntPtr srcB = IntPtr.Add(matB.Data, width * i);

						CopyMemory(IntPtr.Add(ptrR, width * i), srcR, (uint)width);
						CopyMemory(IntPtr.Add(ptrG, width * i), srcG, (uint)width);
						CopyMemory(IntPtr.Add(ptrB, width * i), srcB, (uint)width);
					}
				}

				hImage.GenImage3("byte", width, height, ptrR, ptrG, ptrB);
			}
			else
            {
				return null;
            }

			return hImage;
        }

		public static Mat HImage2Mat(HImage hImage)
		{
			Mat mat = null;
			HTuple htChannels = hImage.CountChannels();
			HTuple width = 0;
			HTuple height = 0;

			if (htChannels.Length == 0)
            {
				mat = null;
            }
			else if (htChannels[0].I == 1)
			{
				HTuple type;
				HTuple ptr = hImage.GetImagePointer1(out type, out width, out height);
				mat = new Mat(new OpenCvSharp.Size(width, height), MatType.CV_8UC1, new Scalar(0));

				unsafe
				{
					for (int i = 0; i < height; i++)
					{
						IntPtr start = IntPtr.Add(mat.Data, width * i);
						CopyMemory(start, new IntPtr((byte*)ptr.IP + width * i), (uint)width);
					}
				}
			}
			else if (htChannels[0].I == 3)
			{
				HTuple ptrR;
				HTuple ptrG;
				HTuple ptrB;
				HTuple type;

				hImage.GetImagePointer3(out ptrR, out ptrG, out ptrB, out type, out width, out height);

				Mat pImageR = new Mat(new OpenCvSharp.Size(width, height), MatType.CV_8UC1);
				Mat pImageG = new Mat(new OpenCvSharp.Size(width, height), MatType.CV_8UC1);
				Mat pImageB = new Mat(new OpenCvSharp.Size(width, height), MatType.CV_8UC1);
				mat = new Mat(new OpenCvSharp.Size(width, height), MatType.CV_8UC3, new Scalar(0, 0, 0));

				unsafe
				{
					for (int i = 0; i < height; i++)
					{
						IntPtr startR = IntPtr.Add(pImageR.Data, width * i);
						IntPtr startG = IntPtr.Add(pImageG.Data, width * i);
						IntPtr startB = IntPtr.Add(pImageB.Data, width * i);
						CopyMemory(startR, new IntPtr((byte*)ptrR.IP + width * i), (uint)width);
						CopyMemory(startG, new IntPtr((byte*)ptrG.IP + width * i), (uint)width);
						CopyMemory(startB, new IntPtr((byte*)ptrB.IP + width * i), (uint)width);
					}
				}

				Mat[] multi = new Mat[] { pImageB, pImageG, pImageR };
				Cv2.Merge(multi, mat);
				pImageR.Dispose();
				pImageG.Dispose();
				pImageB.Dispose();
			}

			return mat;
		}
	}
}
