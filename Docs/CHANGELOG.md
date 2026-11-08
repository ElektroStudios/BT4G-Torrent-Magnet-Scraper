# BT4G Torrent Magnet Scraper Change Log 📋

## v1.1 🔄

### Bug fixes 🐞
- Fixed a crash at startup when the optional `Forbidden Keywords.txt` file was missing (a warning was shown, but the file was read anyway).
- Fixed a crash at startup when the `Query Additional Parameters.txt` file was missing. This file is now optional, and a warning is shown when it is missing.
- Fixed the Cloudflare challenge timeout: 30 minutes were passed in milliseconds to a parameter that expects seconds (about 20 days). This could make the program wait indefinitely when the `cf_clearance` cookie expired in less time than that.
- Fixed the downloadtorrentfile.com Cloudflare warm-up URL, which was built with a backslash (`\`) instead of a forward slash (`/`).
- Fixed HTML entities (e.g., `&amp;`) appearing in torrent names and in Magnet button URLs when they were read from HTML attributes.
- Fixed search terms that contain characters not allowed in file names (e.g., `AC/DC`, `?`, `:`) or reserved device names (e.g., `CON`, `NUL`). Previously no magnet link could be saved for these terms. These characters are now replaced with `_` in the output file name.
- Fixed a possible `OverflowException` that stopped a search term when the "Found N items" counter does not fit in a 32-bit integer.
- Fixed the misaligned item counter column (e.g., `[    5 / 1,234]`) in the console output when there are 1,000 or more results.
- The parent directories of the output and history files are now created if they don't exist.

### Improvements ✨
- Moved the scraping logic that does not need the browser from `Program.vb` to the new `ScraperHelper` module.
- Added a unit test project (MSTest, 180 tests) for the configuration and history parsing, keyword filtering, URL building, HTML parsing, output formatting and the Selenium cache path helpers.

## v1.0 🔄
Initial Release.