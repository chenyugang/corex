﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Corex.IO;

namespace System.IO
{


    public static class Extensions
	{
        public static IEnumerable<string> Lines(this FsPath path)
        {
            using (var reader = new StreamReader(path))
            {
                foreach (var line in reader.Lines())
                    yield return line;
            }
        }
        public static string ReadAllText(this FileInfo file)
        {
            return File.ReadAllText(file.FullName);
        }
        public static IEnumerable<string> Lines(this FileInfo file)
        {
            using (var reader = new StreamReader(file.FullName))
            {
                foreach (var line in reader.Lines())
                    yield return line;
            }
        }
        public static IEnumerable<string> Lines(this StreamReader reader)
        {
            while (true)
            {
                var line = reader.ReadLine();
                if (line == null)
                    yield break;
                yield return line;
            }

        }
        public static FsPath ToFsPath(this string s)
        {
            return new FsPath(s);
        }
        public static void WriteAllText(this FileInfo file, string contents)
        {
            File.WriteAllText(file.FullName, contents);
        }
        public static FileInfo ToFileInfo(this string s)
        {
            return new FileInfo(s);
        }
        public static DirectoryInfo ToDirectoryInfo(this string s)
        {
            return new DirectoryInfo(s);
        }

        public static IEnumerable<DirectoryInfo> Parents(this FileSystemInfo element)
        {
            while (true)
            {
                var parent = element.GetParent();
                if (parent == null)
                    yield break;
                yield return parent;
                element = parent;
            }
        }

		public static DirectoryInfo GetParent(this FileSystemInfo element)
		{
			if (element is FileInfo)
				return ((FileInfo)element).Directory;
			return ((DirectoryInfo)element).Parent;
		}

		public static string GetNameWithoutExtension(this FileInfo file)
		{
			return Path.GetFileNameWithoutExtension(file.Name);
		}
		public static DirectoryInfo GetExistingDirectory(this DirectoryInfo dir, string name)
		{
			var dir2 = new DirectoryInfo(Path.Combine(dir.FullName, name));
			if (dir2.Exists)
				return dir2;
			return null;
		}
		public static DirectoryInfo GetDirectory(this DirectoryInfo dir, string name)
		{
			return new DirectoryInfo(Path.Combine(dir.FullName,name));
		}
		public static DirectoryInfo GetCreateDirectory(this DirectoryInfo dir, string name)
		{
			var sub = dir.GetDirectory(name);
			if (!sub.Exists)
				sub.Create();
			return sub;
		}

		public static FileInfo GetFile(this DirectoryInfo dir, string name)
		{
			return new FileInfo(Path.Combine(dir.FullName + "\\" + name));
		}

		public static void CopyToDirectory(this FileInfo file, string dir)
		{
			var di = new DirectoryInfo(dir);
			file.CopyTo(di.GetFile(file.Name).FullName);
		}

		public static FileInfo CopyToDirectory(this FileInfo file, DirectoryInfo dir)
		{
			return CopyToDirectory(file, dir, false);
		}
		public static FileInfo CopyToDirectory(this FileInfo file, DirectoryInfo dir, bool overwrite)
		{
			var newFile = dir.GetFile(file.Name);
			file.CopyTo(newFile.FullName, overwrite);
			return newFile;
		}

		//public static void Copy(this DirectoryInfo dir, string source, string destination)
		//{
		//  var files = dir.GetFiles(source);
		//  var destDir = new DirectoryInfo(destination);
		//  foreach (var file in files)
		//  {
		//    file.CopyToDirectory(destDir);
		//  }
		//}

		static Stack<DirectoryInfo> GetPath(this DirectoryInfo dir)
		{
			var dirPath = new Stack<DirectoryInfo>();
			while (dir != null)
			{
				dirPath.Push(dir);
				dir = dir.Parent;
			}
			return dirPath;
		}

		public static string CreateRelativePathTo(this DirectoryInfo dir, FileSystemInfo file)
		{
			if (dir.FullName.EqualsIgnoreCase(file.FullName))
				return ".";
			if (file.FullName.StartsWith(dir.FullName, StringComparison.InvariantCultureIgnoreCase))
			{
				return file.FullName.ReplaceFirst(dir.FullName + "\\", "", StringComparison.InvariantCultureIgnoreCase);
			}
			else
			{
				DirectoryInfo dir2 = file as DirectoryInfo;
				if (dir2 == null)
					dir2 = ((FileInfo)file).Directory;
				var dirPath = dir.GetPath();
				var dir2Path = dir2.GetPath();
				while (dirPath.Peek() == dir2Path.Peek())
				{
					dirPath.Pop();
					dir2Path.Pop();
				}
				var final = dirPath.Peek().FullName;
				foreach (var p in dirPath)
				{
					final = Path.Combine(final, "..");
				}
				foreach (var p in dir2Path)
				{
					final = Path.Combine(final, p.Name);
				}
				if (file is FileInfo)
					final = Path.Combine(final, ((FileInfo)file).Name);
				return final;
			}
		}

		public static void Copy(this DirectoryInfo dir, string searchPattern, SearchOption searchOption, DirectoryInfo targetDir, bool overwrite)
		{
			if (overwrite)
				Transform(dir, searchPattern, searchOption, targetDir, CopyWithOverwriteTransformer);
			else
				Transform(dir, searchPattern, searchOption, targetDir, CopyTransformer);
		}

		static void CopyWithOverwriteTransformer(FileInfo source, FileInfo target)
		{
			source.CopyTo(target.FullName, true);
		}
		static void CopyTransformer(FileInfo source, FileInfo target)
		{
			source.CopyTo(target.FullName);
		}

		[Obsolete("Use the other one instead")]
		public static void Transform(
			this DirectoryInfo dir, string searchPattern, SearchOption searchOption,
			DirectoryInfo targetDir,
			Action<FileInfo, FileInfo, Dictionary<string, string>> transformer, Dictionary<string, string> args)
		{
			var files = dir.GetFiles(searchPattern, searchOption);
			foreach (var file in files)
			{
				if (file.FullName.Contains("\\.svn\\"))
					continue;
				var relFile = dir.CreateRelativePathTo(file);
				var finalTargetFile = targetDir.GetFile(relFile);
				transformer(file, finalTargetFile, args);
			}
		}

		public static void Transform(this DirectoryInfo dir, string searchPattern, SearchOption searchOption, DirectoryInfo targetDir, Action<FileInfo, FileInfo> transformer)
		{
			var files = dir.GetFiles(searchPattern, searchOption);
			foreach (var file in files)
			{
				var relFile = dir.CreateRelativePathTo(file);
				var finalTargetFile = targetDir.GetFile(relFile);
				transformer(file, finalTargetFile);
			}
		}


		public static bool IsHidden(this FileInfo file)
		{
			return file.Attributes.HasFlag(FileAttributes.Hidden);
		}
        //public static void SetHidden(this FileInfo file, bool hidden)
        //{
        //    file.Attributes = FlagsHelper.SetOnOrOff(file.Attributes, FileAttributes.Hidden, hidden);
        //}


	}
}
