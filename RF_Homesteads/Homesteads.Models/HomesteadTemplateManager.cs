using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using TaleWorlds.Library;

namespace Homesteads.Models;

public static class HomesteadTemplateManager
{
	private static readonly string TemplatesFolderName = "HomesteadsReloaded_Templates";

	private static readonly string TemplateFilePrefix = "homestead_template_";

	private static readonly int CurrentTemplateVersion = 1;

	private static string _templatesPath = null;

	private static List<HomesteadTemplate> _cachedTemplates = null;

	private static DateTime _cacheTimestamp = DateTime.MinValue;

	public static string GetTemplatesPath()
	{
		if (_templatesPath != null)
		{
			return _templatesPath;
		}
		try
		{
			string text = null;
			try
			{
				PropertyInfo property = Common.PlatformFileHelper.GetType().GetProperty("DocumentsPath", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				if (property != null)
				{
					text = (string)property.GetValue(Common.PlatformFileHelper);
				}
			}
			catch
			{
			}
			if (string.IsNullOrEmpty(text))
			{
				text = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
			}
			_templatesPath = Path.Combine(text, "Mount and Blade II Bannerlord", TemplatesFolderName);
			TraceLogger.Write("HomesteadTemplateManager", "Templates path: " + _templatesPath);
			if (!Directory.Exists(_templatesPath))
			{
				Directory.CreateDirectory(_templatesPath);
				TraceLogger.Write("HomesteadTemplateManager", "Created templates directory at: " + _templatesPath);
			}
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadTemplateManager", "Failed to create templates directory: " + ex.Message);
			_templatesPath = Path.Combine(Path.GetTempPath(), TemplatesFolderName);
			try
			{
				if (!Directory.Exists(_templatesPath))
				{
					Directory.CreateDirectory(_templatesPath);
				}
			}
			catch
			{
				_templatesPath = Path.Combine(Directory.GetCurrentDirectory(), TemplatesFolderName);
			}
		}
		return _templatesPath;
	}

	public static bool SaveTemplate(HomesteadTemplate template)
	{
		if (template == null || string.IsNullOrWhiteSpace(template.Name))
		{
			TraceLogger.Write("HomesteadTemplateManager", "SaveTemplate failed: template is null or has no name");
			return false;
		}
		try
		{
			string templatesPath = GetTemplatesPath();
			TraceLogger.Write("HomesteadTemplateManager", "SaveTemplate: Templates path is: " + templatesPath);
			string text = SanitizeFileName(template.Name);
			string path = TemplateFilePrefix + text + ".json";
			string text2 = Path.Combine(templatesPath, path);
			TraceLogger.Write("HomesteadTemplateManager", "SaveTemplate: Saving to: " + text2);
			string directoryName = Path.GetDirectoryName(text2);
			if (!string.IsNullOrEmpty(directoryName) && !Directory.Exists(directoryName))
			{
				Directory.CreateDirectory(directoryName);
				TraceLogger.Write("HomesteadTemplateManager", "Created templates directory: " + directoryName);
			}
			template.Version = CurrentTemplateVersion;
			string contents = JsonConvert.SerializeObject(template, Formatting.Indented);
			File.WriteAllText(text2, contents);
			TraceLogger.Write("HomesteadTemplateManager", "Saved template '" + template.Name + "' to " + text2);
			_cachedTemplates = null;
			return true;
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadTemplateManager", "Failed to save template '" + template.Name + "': " + ex.Message);
			return false;
		}
	}

	public static HomesteadTemplate LoadTemplate(string name)
	{
		if (string.IsNullOrWhiteSpace(name))
		{
			return null;
		}
		try
		{
			string text = SanitizeFileName(name);
			string path = TemplateFilePrefix + text + ".json";
			string text2 = Path.Combine(GetTemplatesPath(), path);
			if (!File.Exists(text2))
			{
				TraceLogger.Write("HomesteadTemplateManager", "Template file not found: " + text2);
				return null;
			}
			HomesteadTemplate homesteadTemplate = JsonConvert.DeserializeObject<HomesteadTemplate>(File.ReadAllText(text2));
			if (homesteadTemplate != null)
			{
				HomesteadTemplate homesteadTemplate2 = homesteadTemplate;
				if (homesteadTemplate2.TotalCost == null)
				{
					Dictionary<string, int> dictionary = (homesteadTemplate2.TotalCost = new Dictionary<string, int>());
				}
				homesteadTemplate2 = homesteadTemplate;
				if (homesteadTemplate2.Entities == null)
				{
					List<TemplateEntity> list = (homesteadTemplate2.Entities = new List<TemplateEntity>());
				}
				foreach (TemplateEntity entity in homesteadTemplate.Entities)
				{
					if (entity.ItemCosts == null)
					{
						Dictionary<string, int> dictionary = (entity.ItemCosts = new Dictionary<string, int>());
					}
				}
				MigrateTemplateEntities(homesteadTemplate);
			}
			return homesteadTemplate;
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadTemplateManager", "Failed to load template '" + name + "': " + ex.Message);
			return null;
		}
	}

	public static List<HomesteadTemplate> GetAllTemplates(bool forceRefresh = false)
	{
		if (!forceRefresh && _cachedTemplates != null)
		{
			try
			{
				if (new DirectoryInfo(GetTemplatesPath()).LastWriteTime <= _cacheTimestamp)
				{
					return _cachedTemplates;
				}
			}
			catch
			{
				return _cachedTemplates;
			}
		}
		List<HomesteadTemplate> list = new List<HomesteadTemplate>();
		try
		{
			string templatesPath = GetTemplatesPath();
			if (!Directory.Exists(templatesPath))
			{
				_cachedTemplates = list;
				return list;
			}
			string[] files = Directory.GetFiles(templatesPath, TemplateFilePrefix + "*.json");
			foreach (string text in files)
			{
				try
				{
					HomesteadTemplate homesteadTemplate = JsonConvert.DeserializeObject<HomesteadTemplate>(File.ReadAllText(text));
					if (homesteadTemplate == null || string.IsNullOrWhiteSpace(homesteadTemplate.Name))
					{
						continue;
					}
					HomesteadTemplate homesteadTemplate2 = homesteadTemplate;
					if (homesteadTemplate2.TotalCost == null)
					{
						Dictionary<string, int> dictionary = (homesteadTemplate2.TotalCost = new Dictionary<string, int>());
					}
					homesteadTemplate2 = homesteadTemplate;
					if (homesteadTemplate2.Entities == null)
					{
						List<TemplateEntity> list2 = (homesteadTemplate2.Entities = new List<TemplateEntity>());
					}
					foreach (TemplateEntity entity in homesteadTemplate.Entities)
					{
						if (entity.ItemCosts == null)
						{
							Dictionary<string, int> dictionary = (entity.ItemCosts = new Dictionary<string, int>());
						}
					}
					MigrateTemplateEntities(homesteadTemplate);
					list.Add(homesteadTemplate);
				}
				catch (Exception ex)
				{
					TraceLogger.Write("HomesteadTemplateManager", "Failed to load template from " + text + ": " + ex.Message);
				}
			}
			list = (_cachedTemplates = list.OrderBy((HomesteadTemplate t) => t.Name).ToList());
			_cacheTimestamp = DateTime.Now;
		}
		catch (Exception ex2)
		{
			TraceLogger.Write("HomesteadTemplateManager", "Failed to get templates: " + ex2.Message);
		}
		return list;
	}

	public static bool DeleteTemplate(string name)
	{
		if (string.IsNullOrWhiteSpace(name))
		{
			return false;
		}
		try
		{
			string text = SanitizeFileName(name);
			string path = TemplateFilePrefix + text + ".json";
			string path2 = Path.Combine(GetTemplatesPath(), path);
			if (File.Exists(path2))
			{
				File.Delete(path2);
				TraceLogger.Write("HomesteadTemplateManager", "Deleted template '" + name + "'");
				_cachedTemplates = null;
				return true;
			}
			return false;
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadTemplateManager", "Failed to delete template '" + name + "': " + ex.Message);
			return false;
		}
	}

	public static bool TemplateExists(string name)
	{
		if (string.IsNullOrWhiteSpace(name))
		{
			return false;
		}
		string text = SanitizeFileName(name);
		string path = TemplateFilePrefix + text + ".json";
		return File.Exists(Path.Combine(GetTemplatesPath(), path));
	}

	public static bool RenameTemplate(string oldName, string newName)
	{
		HomesteadTemplate homesteadTemplate = LoadTemplate(oldName);
		if (homesteadTemplate == null)
		{
			return false;
		}
		homesteadTemplate.Name = newName;
		if (!SaveTemplate(homesteadTemplate))
		{
			return false;
		}
		string text = SanitizeFileName(oldName);
		string text2 = SanitizeFileName(newName);
		if (text != text2)
		{
			DeleteTemplate(oldName);
		}
		return true;
	}

	public static List<string> GetTemplateNames()
	{
		return (from t in GetAllTemplates()
			select t.Name).ToList();
	}

	public static void ClearCache()
	{
		_cachedTemplates = null;
	}

	private static void MigrateTemplateEntities(HomesteadTemplate? template)
	{
		if (template?.Entities == null)
		{
			return;
		}
		foreach (TemplateEntity entity in template.Entities)
		{
			if (entity == null)
			{
				continue;
			}
			if (entity.PrefabName == "homestead_clay_gatherer")
			{
				string text = entity.DisplayName ?? "";
				if (text.IndexOf("Iron", StringComparison.OrdinalIgnoreCase) >= 0 || text.IndexOf("demir", StringComparison.OrdinalIgnoreCase) >= 0 || text.IndexOf("желез", StringComparison.OrdinalIgnoreCase) >= 0 || text.IndexOf("żelaz", StringComparison.OrdinalIgnoreCase) >= 0 || text.IndexOf("fer", StringComparison.OrdinalIgnoreCase) >= 0 || text.IndexOf("eisen", StringComparison.OrdinalIgnoreCase) >= 0 || text.Contains("铁"))
				{
					entity.PrefabName = "homestead_iron_mine";
					TraceLogger.Write("HomesteadTemplateManager", "Migrated template '" + template.Name + "' entity '" + text + "' prefab to homestead_iron_mine.");
				}
				else if (text.IndexOf("Silver", StringComparison.OrdinalIgnoreCase) >= 0 || text.IndexOf("gümüş", StringComparison.OrdinalIgnoreCase) >= 0 || text.IndexOf("серебр", StringComparison.OrdinalIgnoreCase) >= 0 || text.IndexOf("srebr", StringComparison.OrdinalIgnoreCase) >= 0 || text.IndexOf("argent", StringComparison.OrdinalIgnoreCase) >= 0 || text.IndexOf("silber", StringComparison.OrdinalIgnoreCase) >= 0 || text.Contains("银"))
				{
					entity.PrefabName = "homestead_silver_mine";
					TraceLogger.Write("HomesteadTemplateManager", "Migrated template '" + template.Name + "' entity '" + text + "' prefab to homestead_silver_mine.");
				}
			}
			else if (entity.PrefabName == "homestead_native_decorated_work_table")
			{
				string text2 = entity.DisplayName ?? "";
				if (text2.IndexOf("Jewel", StringComparison.OrdinalIgnoreCase) >= 0 || text2.IndexOf("kuyum", StringComparison.OrdinalIgnoreCase) >= 0 || text2.IndexOf("ювелир", StringComparison.OrdinalIgnoreCase) >= 0 || text2.IndexOf("jubil", StringComparison.OrdinalIgnoreCase) >= 0 || text2.IndexOf("bijout", StringComparison.OrdinalIgnoreCase) >= 0 || text2.IndexOf("juwel", StringComparison.OrdinalIgnoreCase) >= 0 || text2.Contains("珠") || text2.Contains("首饰") || text2.Contains("宝"))
				{
					entity.PrefabName = "homestead_silversmith";
					TraceLogger.Write("HomesteadTemplateManager", "Migrated template '" + template.Name + "' entity '" + text2 + "' prefab to homestead_silversmith.");
				}
			}
		}
	}

	private static string SanitizeFileName(string name)
	{
		if (string.IsNullOrWhiteSpace(name))
		{
			return "unnamed";
		}
		char[] invalidFileNameChars = Path.GetInvalidFileNameChars();
		string text = name;
		char[] array = invalidFileNameChars;
		foreach (char oldChar in array)
		{
			text = text.Replace(oldChar, '_');
		}
		text = text.Replace(' ', '_');
		while (text.Contains("__"))
		{
			text = text.Replace("__", "_");
		}
		text = text.Trim(new char[1] { '_' });
		if (text.Length > 50)
		{
			text = text.Substring(0, 50);
		}
		return text.ToLowerInvariant();
	}
}
