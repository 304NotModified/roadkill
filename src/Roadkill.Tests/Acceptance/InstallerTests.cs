using System;
using System.Configuration;
using System.IO;
using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using Roadkill.Core;
using Roadkill.Core.Database;

namespace Roadkill.Tests.Acceptance
{
	[TestFixture]
	[Category("Acceptance")]
	public class InstallerTests : AcceptanceTestBase
	{
		[TestFixtureSetUp]
		public void TestFixtureSetUp()
		{
			ConfigFileManager.CopyConnectionStringsConfig();
			SqlCeSetup.CopyDb();
			SqliteSetup.CopyDb();
		}

		[SetUp]
		public void Setup()
		{
			Driver.Manage().Timeouts().ImplicitlyWait(TimeSpan.FromSeconds(10)); // for ajax calls
			UpdateWebConfig();
			Console.WriteLine("~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~");
		}

		[TestFixtureTearDown]
		public void TearDown()
		{
			Console.WriteLine("~~~~~~~~~~~~ Installer acceptance tests teardown ~~~~~~~~~~~~");
			string sitePath = Settings.WEB_PATH;

			try
			{
				// Remove any attachment folders used by the installer tests
				string installerTestsAttachmentsPath = Path.Combine(sitePath, "AcceptanceTests");
				Directory.Delete(installerTestsAttachmentsPath, true);
				Console.WriteLine("Deleted temp attachment folders for installer tests");
			}
			catch { }

			// Reset the db and web.config back for all other acceptance tests
			SqlServerSetup.RecreateLocalDbData();
			ConfigFileManager.CopyConnectionStringsConfig();
			Console.WriteLine("Copied databases and web.config back for installer tests");
			Console.WriteLine("~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~");
		}

		private void UpdateWebConfig()
		{
			string sitePath = Settings.WEB_PATH;
			string webConfigPath = Path.Combine(sitePath, "web.config");
			string roadkillConfigPath = Path.Combine(sitePath, "roadkill.config");

			// Remove the readonly flag from one of the installer tests (this could be fired in any order)
			File.SetAttributes(webConfigPath, FileAttributes.Normal);
			File.SetAttributes(roadkillConfigPath, FileAttributes.Normal);

			// Switch installed=false in the web.config (roadkill.config)
			ExeConfigurationFileMap fileMap = new ExeConfigurationFileMap();
			fileMap.ExeConfigFilename = webConfigPath;
			System.Configuration.Configuration config = ConfigurationManager.OpenMappedExeConfiguration(fileMap, ConfigurationUserLevel.None);
			RoadkillSection section = config.GetSection("roadkill") as RoadkillSection;

			section.Installed = false;
			config.ConnectionStrings.ConnectionStrings["Roadkill"].ConnectionString = "";
			config.Save(ConfigurationSaveMode.Minimal);

			Console.WriteLine("Updated {0} for installer tests", webConfigPath);
		}

		[Test]
		public void Installation_Page_Should_Display_For_Home_Page_When_Installed_Is_False()
		{
			// Arrange


			// Act
			Driver.Navigate().GoToUrl(BaseUrl);

			// Assert
			Assert.That(Driver.FindElements(By.CssSelector("div#installer-container")).Count, Is.EqualTo(1));
		}

		[Test]
		public void Installation_Page_Should_Display_For_Login_Page_When_Installed_Is_False()
		{
			// Arrange
			

			// Act
			Driver.Navigate().GoToUrl(LoginUrl);

			// Assert
			Assert.That(Driver.FindElements(By.CssSelector("div#installer-container")).Count, Is.EqualTo(1));
		}

		[Test]
		public void Language_Selection_Should_Display_For_First_Page()
		{
			// Arrange

			// Act
			Driver.Navigate().GoToUrl(BaseUrl);

			// Assert
			Assert.That(Driver.FindElements(By.CssSelector("ul#language li")).Count, Is.GreaterThanOrEqualTo(1));
			Assert.That(Driver.FindElements(By.CssSelector("ul#language li"))[0].Text, Is.EqualTo("English"));
		}

		[Test]
		public void Step1_Web_Config_Test_Button_Should_Display_Success_Toast()
		{
			// Arrange
			Driver.Navigate().GoToUrl(BaseUrl);

			// Act
			ClickLanguageLink();
			Driver.FindElement(By.CssSelector("button[id=testwebconfig]")).Click();

			// Assert
			Assert.That(Driver.IsElementDisplayed(By.CssSelector("#toast-container")), Is.True);
		}

		[Test]
		public void Step1_Web_Config_Test_Button_Should_Display_Error_Modal_And_No_Continue_Link_For_Readonly_Webconfig()
		{
			// Arrange
			string sitePath = Settings.WEB_PATH;
			string webConfigPath = Path.Combine(sitePath, "web.config");
			File.SetAttributes(webConfigPath, FileAttributes.ReadOnly);

			// Cascades down
			string roadkillConfigPath = Path.Combine(sitePath, "roadkill.config");
			File.SetAttributes(roadkillConfigPath, FileAttributes.ReadOnly);

			Driver.Navigate().GoToUrl(BaseUrl);
			ClickLanguageLink();

			// Act
			Driver.FindElement(By.CssSelector("button[id=testwebconfig]")).Click();

			// Assert
			Assert.That(Driver.IsElementDisplayed(By.CssSelector(".bootbox")), Is.True);
		}

		[Test]
		public void Step2_Connection_Test_Button_Should_Display_Success_Toast_For_Good_ConnectionString()
		{
			// Arrange
			Driver.Navigate().GoToUrl(BaseUrl);
			ClickLanguageLink();

			// Act
			Driver.FindElement(By.CssSelector("button[id=testwebconfig]")).Click();
			Driver.WaitForElementDisplayed(By.CssSelector("#bottom-buttons > a")).Click();

			SelectElement select = new SelectElement(Driver.FindElement(By.Id("DataStoreTypeName")));
			select.SelectByValue(DataStoreType.SqlServerCe.Name);

			Driver.FindElement(By.Id("ConnectionString")).SendKeys(@"Data Source=|DataDirectory|\roadkill-acceptancetests.sdf");
			Driver.FindElement(By.CssSelector("button[id=testdbconnection]")).Click();

			// Assert
			Assert.That(Driver.IsElementDisplayed(By.CssSelector("#toast-container")), Is.True);
		}

		[Test]
		public void Step2_Connection_Test_Button_Should_Display_Error_Modal_For_Bad_ConnectionString()
		{
			// Arrange
			Driver.Navigate().GoToUrl(BaseUrl);
			ClickLanguageLink();

			// Act
			Driver.FindElement(By.CssSelector("button[id=testwebconfig]")).Click();
			Driver.WaitForElementDisplayed(By.CssSelector("#bottom-buttons > a")).Click();

			SelectElement select = new SelectElement(Driver.FindElement(By.Id("DataStoreTypeName")));
			select.SelectByValue(DataStoreType.SqlServerCe.Name);

			Driver.FindElement(By.Id("ConnectionString")).SendKeys(@"Data Source=|DataDirectory|\madeupfilename.sdf");
			Driver.FindElement(By.CssSelector("button[id=testdbconnection]")).Click();

			// Assert
			Assert.That(Driver.IsElementDisplayed(By.CssSelector(".bootbox")), Is.True);

		}

		[Test]
		public void Step2_Missing_Site_Name_Title_Should_Prevent_Continue()
		{
			// Arrange
			Driver.Navigate().GoToUrl(BaseUrl);
			ClickLanguageLink();

			// Act
			Driver.FindElement(By.CssSelector("button[id=testwebconfig]")).Click();
			Driver.WaitForElementDisplayed(By.CssSelector("#bottom-buttons > a")).Click();

			Driver.FindElement(By.Id("SiteName")).Clear();
			Driver.FindElement(By.Id("SiteUrl")).SendKeys("not empty");
			Driver.FindElement(By.Id("ConnectionString")).SendKeys("not empty");
			Driver.FindElement(By.CssSelector("div.continue button")).Click();

			// Assert
			Assert.That(Driver.IsElementDisplayed(By.CssSelector(".help-block")), Is.True);
			Assert.That(Driver.FindElement(By.Id("SiteName")).Displayed, Is.True);
		}

		[Test]
		public void Step2_Missing_Site_Url_Should_Prevent_Continue()
		{
			// Arrange
			Driver.Navigate().GoToUrl(BaseUrl);
			ClickLanguageLink();

			// Act
			Driver.FindElement(By.CssSelector("button[id=testwebconfig]")).Click();
			Driver.WaitForElementDisplayed(By.CssSelector("#bottom-buttons > a")).Click();

			Driver.FindElement(By.Id("SiteName")).SendKeys("not empty");
			Driver.FindElement(By.Id("SiteUrl")).Clear();
			Driver.FindElement(By.Id("ConnectionString")).SendKeys("not empty");
			Driver.FindElement(By.CssSelector("div.continue button")).Click();

			// Assert
			Assert.That(Driver.IsElementDisplayed(By.CssSelector(".help-block")), Is.True);
			Assert.That(Driver.FindElement(By.Id("SiteUrl")).Displayed, Is.True);
		}

		[Test]
		public void Step2_Missing_ConnectionString_Should_Prevent_Contine()
		{
			// Arrange
			Driver.Navigate().GoToUrl(BaseUrl);
			ClickLanguageLink();

			// Act
			Driver.FindElement(By.CssSelector("button[id=testwebconfig]")).Click();
			Driver.WaitForElementDisplayed(By.CssSelector("#bottom-buttons > a")).Click();

			Driver.FindElement(By.Id("SiteName")).SendKeys("not empty");
			Driver.FindElement(By.Id("SiteUrl")).SendKeys("not empty");
			Driver.FindElement(By.Id("ConnectionString")).Clear();
			Driver.FindElement(By.CssSelector("div.continue button")).Click();

			// Assert
			Assert.That(Driver.IsElementDisplayed(By.CssSelector(".help-block")), Is.True);
			Assert.That(Driver.FindElement(By.Id("ConnectionString")).Displayed, Is.True);
		}

		[Test]
		public void Step3_Missing_Admin_Email_Should_Prevent_Continue()
		{
			// Arrange
			Driver.Navigate().GoToUrl(BaseUrl);
			ClickLanguageLink();

			// Act
			Driver.FindElement(By.CssSelector("button[id=testwebconfig]")).Click();
			Driver.WaitForElementDisplayed(By.CssSelector("#bottom-buttons > a")).Click();

			Driver.FindElement(By.Id("SiteName")).SendKeys("not empty");
			Driver.FindElement(By.Id("SiteUrl")).SendKeys("not empty");
			Driver.FindElement(By.Id("ConnectionString")).SendKeys("not empty");
			Driver.FindElement(By.CssSelector("div.continue button")).Click();

			Driver.FindElement(By.CssSelector("div.continue button")).Click();

			Driver.FindElement(By.Id("AdminEmail")).Clear();
			Driver.FindElement(By.Id("AdminPassword")).SendKeys("not empty");
			Driver.FindElement(By.Id("password2")).SendKeys("not empty");
			Driver.FindElement(By.CssSelector("div.continue button")).Click();

			// Assert
			Assert.That(Driver.IsElementDisplayed(By.CssSelector(".help-block")), Is.True);
			Assert.That(Driver.FindElement(By.Id("AdminEmail")).Displayed, Is.True);
		}

		[Test]
		public void Step3_Missing_Admin_Password_Should_Prevent_Continue()
		{
			// Arrange
			Driver.Navigate().GoToUrl(BaseUrl);
			ClickLanguageLink();

			// Act
			Driver.FindElement(By.CssSelector("button[id=testwebconfig]")).Click();
			Driver.WaitForElementDisplayed(By.CssSelector("#bottom-buttons > a")).Click();

			Driver.FindElement(By.Id("SiteName")).SendKeys("not empty");
			Driver.FindElement(By.Id("SiteUrl")).SendKeys("not empty");
			Driver.FindElement(By.Id("ConnectionString")).SendKeys("not empty");
			Driver.FindElement(By.CssSelector("div.continue button")).Click();

			Driver.FindElement(By.CssSelector("div.continue button")).Click();

			Driver.FindElement(By.Id("AdminEmail")).SendKeys("not empty");
			Driver.FindElement(By.Id("AdminPassword")).Clear();
			Driver.FindElement(By.Id("password2")).SendKeys("not empty");
			Driver.FindElement(By.CssSelector("div.continue button")).Click();

			// Assert
			Assert.That(Driver.IsElementDisplayed(By.CssSelector(".help-block")), Is.True);
			Assert.That(Driver.FindElement(By.Id("AdminPassword")).Displayed, Is.True);
		}

		[Test]
		public void Step3_Not_Min_Length_Admin_Password_Should_Prevent_Continue()
		{
			// Arrange
			Driver.Navigate().GoToUrl(BaseUrl);
			ClickLanguageLink();

			// Act
			Driver.FindElement(By.CssSelector("button[id=testwebconfig]")).Click();
			Driver.WaitForElementDisplayed(By.CssSelector("#bottom-buttons > a")).Click();

			Driver.FindElement(By.Id("SiteName")).SendKeys("not empty");
			Driver.FindElement(By.Id("SiteUrl")).SendKeys("not empty");
			Driver.FindElement(By.Id("ConnectionString")).SendKeys("not empty");
			Driver.FindElement(By.CssSelector("div.continue button")).Click();

			Driver.FindElement(By.CssSelector("div.continue button")).Click();

			Driver.FindElement(By.Id("AdminEmail")).SendKeys("not empty");
			Driver.FindElement(By.Id("AdminPassword")).SendKeys("1");
			Driver.FindElement(By.Id("password2")).SendKeys("not empty");
			Driver.FindElement(By.CssSelector("div.continue button")).Click();

			// Assert
			Assert.That(Driver.IsElementDisplayed(By.CssSelector(".help-block")), Is.True);
			Assert.That(Driver.FindElement(By.Id("AdminPassword")).Displayed, Is.True);
		}

		[Test]
		public void Step3_Missing_Admin_Password2_Should_Prevent_Continue()
		{
			// Arrange
			Driver.Navigate().GoToUrl(BaseUrl);
			ClickLanguageLink();

			// Act
			Driver.FindElement(By.CssSelector("button[id=testwebconfig]")).Click();
			Driver.WaitForElementDisplayed(By.CssSelector("#bottom-buttons > a")).Click();

			Driver.FindElement(By.Id("SiteName")).SendKeys("not empty");
			Driver.FindElement(By.Id("SiteUrl")).SendKeys("not empty");
			Driver.FindElement(By.Id("ConnectionString")).SendKeys("not empty");
			Driver.FindElement(By.CssSelector("div.continue button")).Click();

			Driver.FindElement(By.CssSelector("div.continue button")).Click();

			Driver.FindElement(By.Id("AdminEmail")).SendKeys("not empty");
			Driver.FindElement(By.Id("AdminPassword")).SendKeys("not empty");
			Driver.FindElement(By.Id("password2")).Clear();
			Driver.FindElement(By.CssSelector("div.continue button")).Click();

			// Assert
			Assert.That(Driver.IsElementDisplayed(By.CssSelector(".help-block")), Is.True);
			Assert.That(Driver.FindElement(By.Id("password2")).Displayed, Is.True);
		}

		[Test]
		public void Step4_Test_Attachments_Folder_Button_With_Existing_Folder_Should_Display_Success_Toast()
		{
			// Arrange
			string sitePath = Settings.WEB_PATH;
			Guid folderGuid = Guid.NewGuid();
			string attachmentsFolder = Path.Combine(sitePath, "AcceptanceTests", folderGuid.ToString());		
			Directory.CreateDirectory(attachmentsFolder);

			Driver.Navigate().GoToUrl(BaseUrl);
			ClickLanguageLink();

			// Act
			Driver.FindElement(By.CssSelector("button[id=testwebconfig]")).Click();
			Driver.WaitForElementDisplayed(By.CssSelector("#bottom-buttons > a")).Click();

			Driver.FindElement(By.Id("SiteName")).SendKeys("not empty");
			Driver.FindElement(By.Id("SiteUrl")).SendKeys("not empty");
			Driver.FindElement(By.Id("ConnectionString")).SendKeys("not empty");
			Driver.FindElement(By.CssSelector("div.continue button")).Click();

			Driver.FindElement(By.CssSelector("div.continue button")).Click();

			Driver.FindElement(By.Id("AdminEmail")).SendKeys("admin@localhost");
			Driver.FindElement(By.Id("AdminPassword")).SendKeys("not empty");
			Driver.FindElement(By.Id("password2")).SendKeys("not empty");
			Driver.FindElement(By.CssSelector("div.continue button")).Click();

			Driver.FindElement(By.Id("AttachmentsFolder")).Clear();
			Driver.FindElement(By.Id("AttachmentsFolder")).SendKeys("~/AcceptanceTests/" + folderGuid);
			Driver.FindElement(By.CssSelector("button[id=testattachments]")).Click();

			// Assert
			try
			{
				Assert.That(Driver.IsElementDisplayed(By.CssSelector("#toast-container")), Is.True);
			}
			finally
			{
				Directory.Delete(attachmentsFolder, true);
			}
		}

		[Test]
		public void Step4_Test_Attachments_Folder_Button_With_Missing_Folder_Should_Display_Failure_Modal()
		{
			// Arrange
			Guid folderGuid = Guid.NewGuid();
			Driver.Navigate().GoToUrl(BaseUrl);
			ClickLanguageLink();

			// Act
			Driver.FindElement(By.CssSelector("button[id=testwebconfig]")).Click();
			Driver.WaitForElementDisplayed(By.CssSelector("#bottom-buttons > a")).Click();

			Driver.FindElement(By.Id("SiteName")).SendKeys("not empty");
			Driver.FindElement(By.Id("SiteUrl")).SendKeys("not empty");
			Driver.FindElement(By.Id("ConnectionString")).SendKeys("not empty");
			Driver.FindElement(By.CssSelector("div.continue button")).Click();

			Driver.FindElement(By.CssSelector("div.continue button")).Click();

			Driver.FindElement(By.Id("AdminEmail")).SendKeys("admin@localhost");
			Driver.FindElement(By.Id("AdminPassword")).SendKeys("not empty");
			Driver.FindElement(By.Id("password2")).SendKeys("not empty");
			Driver.FindElement(By.CssSelector("div.continue button")).Click();

			Driver.FindElement(By.Id("AttachmentsFolder")).Clear();
			Driver.FindElement(By.Id("AttachmentsFolder")).SendKeys("~/" + folderGuid);
			Driver.FindElement(By.CssSelector("button[id=testattachments]")).Click();

			// Assert
			Assert.That(Driver.IsElementDisplayed(By.CssSelector(".bootbox")), Is.True);
		}

		[Test]
		public void Navigation_Persists_Field_Values_Correctly()
		{
			// Arrange
			string sitePath = Settings.WEB_PATH;
			Guid folderGuid = Guid.NewGuid();
			Driver.Navigate().GoToUrl(BaseUrl);
			ClickLanguageLink();

			// Act
			Driver.FindElement(By.CssSelector("button[id=testwebconfig]")).Click();
			Driver.WaitForElementDisplayed(By.CssSelector("#bottom-buttons > a")).Click();

			Driver.FindElement(By.Id("SiteName")).Clear();
			Driver.FindElement(By.Id("SiteName")).SendKeys("Site Name");

			Driver.FindElement(By.Id("SiteUrl")).Clear();
			Driver.FindElement(By.Id("SiteUrl")).SendKeys("Site Url");

			Driver.FindElement(By.Id("ConnectionString")).Clear();
			Driver.FindElement(By.Id("ConnectionString")).SendKeys("Connection String");
			SelectElement select = new SelectElement(Driver.FindElement(By.Id("DataStoreTypeName")));
			select.SelectByValue(DataStoreType.MySQL.Name);

			Driver.FindElement(By.CssSelector("div.continue button")).Click();
			Driver.FindElement(By.CssSelector("div.continue button")).Click();

			Driver.FindElement(By.CssSelector("div.previous a")).Click();
			Driver.FindElement(By.CssSelector("div.previous a")).Click();

			// Assert
			Assert.That(Driver.FindElement(By.Id("SiteName")).GetAttribute("value"), Is.EqualTo("Site Name"));
			Assert.That(Driver.FindElement(By.Id("SiteUrl")).GetAttribute("value"), Is.EqualTo("Site Url"));
			Assert.That(Driver.FindElement(By.Id("ConnectionString")).GetAttribute("value"), Is.EqualTo("Connection String"));

			select = new SelectElement(Driver.FindElement(By.Id("DataStoreTypeName")));
			Assert.That(select.SelectedOption.GetAttribute("value"), Is.EqualTo(DataStoreType.MySQL.Name));
		}

		[Test]
		public void All_Steps_With_Minimum_Required_SqlServerCE_Should_Complete()
		{
			// Arrange
			Driver.Navigate().GoToUrl(BaseUrl);
			ClickLanguageLink();

			//
			// ***Act***
			//

			// step 1
			Driver.FindElement(By.CssSelector("button[id=testwebconfig]")).Click();
			Driver.WaitForElementDisplayed(By.CssSelector("#bottom-buttons > a")).Click();

			// step 2
			Driver.FindElement(By.Id("SiteName")).SendKeys("Acceptance tests");
			SelectElement select = new SelectElement(Driver.FindElement(By.Id("DataStoreTypeName")));
			select.SelectByValue(DataStoreType.SqlServerCe.Name);

			Driver.FindElement(By.Id("ConnectionString")).SendKeys(@"Data Source=|DataDirectory|\roadkill-acceptancetests.sdf");
			Driver.FindElement(By.CssSelector("div.continue button")).Click();

			// step 3
			Driver.FindElement(By.CssSelector("div.continue button")).Click();

			// step 3b
			Driver.FindElement(By.Id("AdminEmail")).SendKeys("admin@localhost");
			Driver.FindElement(By.Id("AdminPassword")).SendKeys("password");
			Driver.FindElement(By.Id("password2")).SendKeys("password");
			Driver.FindElement(By.CssSelector("div.continue button")).Click();

			// step 4
			Driver.FindElement(By.CssSelector("input[id=UseObjectCache]")).Click();
			Driver.FindElement(By.CssSelector("div.continue button")).Click();

			// step5
			Assert.That(Driver.FindElement(By.CssSelector(".alert strong")).Text, Is.EqualTo("Installation successful"), Driver.PageSource);
			Driver.FindElement(By.CssSelector(".continue a")).Click();

			// login, create a page
			LoginAsAdmin();
			CreatePageWithTitleAndTags("Homepage", "homepage");

			//
			// ***Assert***
			//
			Driver.Navigate().GoToUrl(BaseUrl);
			Assert.That(Driver.FindElement(By.CssSelector(".pagetitle")).Text, Contains.Substring("Homepage"));
			Assert.That(Driver.FindElement(By.CssSelector("#pagecontent p")).Text, Contains.Substring("Some content goes here"));
		}

		[Test]
		[Description("These tests go through the entire installer workflow to ensure no localization strings break the installer.")]
		[TestCase(Language.English)]
		[TestCase(Language.Czech)]
		[TestCase(Language.Dutch)]
		[TestCase(Language.German)]
		[TestCase(Language.Hindi)]
		[TestCase(Language.Italian)]
		[TestCase(Language.Polish)]
		[TestCase(Language.Portuguese)]
		[TestCase(Language.Russian)]
		[TestCase(Language.Spanish)]
		[TestCase(Language.Swedish)]
		public void All_Steps_With_Minimum_Required_SQLServer2012_Should_Complete(Language language)
		{
			// Arrange
			Driver.Navigate().GoToUrl(BaseUrl);
			ClickLanguageLink(language);

			//
			// ***Act***
			//

			// step 1
			Driver.FindElement(By.CssSelector("button[id=testwebconfig]")).Click();
			Driver.WaitForElementDisplayed(By.CssSelector("#bottom-buttons > a")).Click();

			// step 2
			Driver.FindElement(By.Id("SiteName")).SendKeys("Acceptance tests");
			SelectElement select = new SelectElement(Driver.FindElement(By.Id("DataStoreTypeName")));
			select.SelectByValue(DataStoreType.SqlServer2012.Name);

			Driver.FindElement(By.Id("ConnectionString")).SendKeys(@"Server=(LocalDB)\v11.0;Integrated Security=true;");
			Driver.FindElement(By.CssSelector("div.continue button")).Click();

			// step 3
			Driver.FindElement(By.CssSelector("div.continue button")).Click();

			// step 3b
			Driver.FindElement(By.Id("AdminEmail")).SendKeys("admin@localhost");
			Driver.FindElement(By.Id("AdminPassword")).SendKeys("password");
			Driver.FindElement(By.Id("password2")).SendKeys("password");
			Driver.FindElement(By.CssSelector("div.continue button")).Click();

			// step 4
			Driver.FindElement(By.CssSelector("input[id=UseObjectCache]")).Click();
			Driver.FindElement(By.CssSelector("div.continue button")).Click();

			// step5
			Driver.FindElement(By.CssSelector(".continue a")).Click();

			// login, create a page
			LoginAsAdmin();
			CreatePageWithTitleAndTags("Homepage", "homepage");

			//
			// ***Assert***
			//
			Driver.Navigate().GoToUrl(BaseUrl);
			Assert.That(Driver.FindElement(By.CssSelector(".pagetitle")).Text, Contains.Substring("Homepage"));
			Assert.That(Driver.FindElement(By.CssSelector("#pagecontent p")).Text, Contains.Substring("Some content goes here"));
		}

		[Test]
		[Description("These tests ensure nothing has gone wrong with the localization satellites assemblies/VS project")]
		[TestCase(Language.English, "Thank for you downloading Roadkill .NET Wiki engine")]
		[TestCase(Language.Czech, "Děkujeme že jste si stáhli Roadkill .NET Wiki")]
		[TestCase(Language.Dutch, "Bedankt voor het downloaden van Roadkill. NET Wiki engine. De installatie schrijft de gemaakte instellingen naar de web.config en de database.")]
		[TestCase(Language.German, "Danke, dass Sie Roadkill .NET Wiki-Engine herunterladen")]
		[TestCase(Language.Hindi, "आप Roadkill. नेट विकी इंजन डाउनलोड करने के लिए धन्यवा")]
		[TestCase(Language.Italian, "Grazie per il download di motore wiki NET Roadkill")]
		[TestCase(Language.Polish, "Dziękujemy za zainstalowanie platformy Roadkill .NET Wiki")]
		[TestCase(Language.Portuguese, "Obrigado para você fazer o download Roadkill. Engine NET Wiki")]
		[TestCase(Language.Russian, "Спасибо за загрузку вики-движка Roadkill .NET. Мастер установки сохранить настройки которые вы укажете в файл web.config (а также в базу данных).")]
		[TestCase(Language.Spanish, "Gracias por su descarga de Roadkill. Motor Wiki NET")]
		[TestCase(Language.Swedish, "Tack för att du laddat ned Roadkill .NET Wiki")]
		public void Language_Screen_Should_Contain_Expected_Text_In_Step_1(Language language, string expectedText)
		{
			// Arrange
			Driver.Navigate().GoToUrl(BaseUrl);

			// Act
			ClickLanguageLink(language);

			// Assert
			Assert.That(Driver.FindElement(By.CssSelector("#content > p")).Text, Contains.Substring(expectedText));
		}

		protected void ClickLanguageLink(Language language = Language.English)
		{
			int index = (int)language;
			Driver.FindElements(By.CssSelector("ul#language a"))[index].Click();
		}

		/// <summary>
		/// The number equates the index in the list of languages, which is ordered by the language name,
		/// e.g. German is Deutsch.
		/// </summary>
		public enum Language
		{
			English = 0,
			Czech = 1,
			German = 2,
			Dutch = 3,
			Spanish = 4,
			Hindi = 5,
			Italian = 6,	
			Polish = 7,
			Portuguese = 8,
			Russian = 9,
			Swedish = 10
		}
	}
}
