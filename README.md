<!-- Common Project Tags:
command-line 
console-applications 
desktop-app 
desktop-application 
dotnet 
dotnet-core 
netcore 
netframework 
netframework48 
tool 
tools 
vbnet 
visualstudio 
windows 
windows-app 
windows-application 
windows-applications 
windows-forms 
winforms 
torrent 
magnet 
magnets 
BT4G 
searcher 
selenium 
scrap 
scraper 
 -->

# BT4G Torrent Magnet Scraper

### An automated interactive command-line torrent magnet scraper for https://bt4gprx.com/ with rule-based filtering.

------------------

## 👋 Introduction

**BT4G Torrent Magnet Scraper** is an automated command-line tool developed in VB.NET for .NET Framework 4.8. It is designed to programmatically query the https://bt4gprx.com/ search engine, parse the HTML responses, and extract torrent magnet URIs. The application streamlines bulk scraping workflows through a strict text-based keyword filtering system, ensuring you only gather the exact magnet links you need while automatically discarding unwanted results.

The program runs Selenium in the background to launch and automate a Chrome browser instance.

## 👌 Features

* **Sequential Bulk Querying:** Processes multiple search terms automatically from a structured input list.
* **Smart Four-File Filtering:**
  * `.\Config\Search Terms.txt`: Defines the target queries to execute on the website (one per line).
  * `.\Config\Required Keywords.txt`: Validates results against a whitelist; unmatched items are automatically skipped.
  * `.\Config\Forbidden Keywords.txt`: Blacklists unwanted terms, instantly dropping matching torrents even if they satisfy the required criteria.
  * `.\Config\Query Additional Parameters.txt`: Appends custom HTTP GET URL parameters (such as `category=movie` or `orderby=seeders`) to filter and sort results directly on the server side.
  
## 🖼️ Screenshots

![screenshot](/Images/screenshot1.png)

## 🎦 Videos

[BT4G Torrent Magnet Scraper demo.mp4](https://github.com/user-attachments/assets/6693987c-c059-4955-ae73-6b1d91cdacbe)

## 📝 Requirements

- Microsoft Windows OS (64-Bit).

## 🤖 Getting Started

Download the latest compilation by clicking [here](https://github.com/ElektroStudios/BT4G-Torrent-Magnet-Scraper/releases/latest) and start using it.

Also, you can read a manual in HTML format by clicking [here](https://htmlpreview.github.io/?https://github.com/ElektroStudios/BT4G-Torrent-Magnet-Scraper/blob/main/Source/BT4G%20Scraper/manual/BT4G%20Torrent%20Magnet%20Scraper%20Manual.html).

## 💡 Performance Recommendation

To significantly optimize the page loading speed and reduce bandwidth consumption when accessing the bt4g website, it is **highly recommended** to install/load the **uBlock Origin Lite** extension in the Chrome browser instance. By actively blocking unwanted ads, trackers, and heavy background scripts, the scraping process becomes drastically faster and much more stable.

## 🔄 Change Log

Explore the complete list of changes, bug fixes, and improvements across different releases by clicking [here](/Docs/CHANGELOG.md).

## 🏆 Credits

This work relies on the following resources: 

 - [.NET Framework](https://dotnet.microsoft.com/en-us/download/dotnet-framework)
 - [Selenium.WebDriver](https://www.nuget.org/packages/selenium.webdriver)
 - [HtmlAgilityPack](https://www.nuget.org/packages/HtmlAgilityPack)
 - [DevCase](https://github.com/ElektroStudios/DevCase.github.io)

## ⚠️ Disclaimer:

This Work (the repository and the content provided in) is provided "as is", without warranty of any kind, express or implied, including but not limited to the warranties of merchantability, fitness for a particular purpose and noninfringement. In no event shall the authors or copyright holders be liable for any claim, damages or other liability, whether in an action of contract, tort or otherwise, arising from, out of or in connection with the Work or the use or other dealings in the Work.

This Work has no affiliation, approval or endorsement by the author(s) of the third-party libraries used by this Work.

The author of this software, ElektroStudios, explicitly disclaims any responsibility for the content acquired using the extracted magnet links. The user assumes full liability for ensuring that their torrent downloads comply with all applicable local laws and regulations.

## 💪 Contributing

Your contribution is highly appreciated!. If you have any ideas, suggestions, or encounter issues, feel free to open an issue by clicking [here](https://github.com/ElektroStudios/BT4G-Torrent-Magnet-Scraper/issues/new/choose). 

Your input helps make this Work better for everyone. Thank you for your support! 🚀

## 💰 Beyond Contribution 

This work is distributed for educational purposes and without any profit motive. However, if you find value in my efforts and wish to support and motivate my ongoing work, you may consider contributing financially through the following options:

<br></br>
<p align="center"><img src="/Images/github_circle.png" height=100></p>
<p align="center">__________________</p>
<h3 align="center">Becoming my sponsor on Github:</h3>
<p align="center">You can show me your support by clicking <a href="https://github.com/sponsors/ElektroStudios/">here</a>, <br align="center">contributing any amount you prefer, and unlocking rewards!</br></p>
<br></br>

<p align="center"><img src="/Images/paypal_circle.png" height=100></p>
<p align="center">__________________</p>
<h3 align="center">Making a Paypal Donation:</h3>
<p align="center">You can donate to me any amount you like via Paypal by clicking <a href="https://www.paypal.com/cgi-bin/webscr?cmd=_s-xclick&hosted_button_id=E4RQEV6YF5NZY">here</a>.</p>
<br></br>

<p align="center"><img src="/Images/envato_circle.png" height=100></p>
<p align="center">__________________</p>
<h3 align="center">Purchasing software of mine at Envato's Codecanyon marketplace:</h3>
<p align="center">If you are a .NET developer, you may want to explore '<b>DevCase Class Library for .NET</b>', <br align="center">a huge set of APIs that I have on sale. Check out the product by clicking <a href="https://codecanyon.net/item/elektrokit-class-library-for-net/19260282">here</a></br><br align="center"><i>It also contains all piece of reusable code that you can find across the source code of my open source works.</i></p>
<br></br>

<h2 align="center"><u>Your support means the world to me! Thank you for considering it!</u> 👍</h2>
