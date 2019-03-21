# FileWatcher for Database Restore (Windows Service)
[![FOSSA Status](https://app.fossa.io/api/projects/git%2Bgithub.com%2Fhirocsingh%2FFileWatcher-for-DB-Restore.svg?type=shield)](https://app.fossa.io/projects/git%2Bgithub.com%2Fhirocsingh%2FFileWatcher-for-DB-Restore?ref=badge_shield)

A Demo FileWatcher used for Watching a specific folder for .bak files Pick it up and Restore the Database.  After Restoration is complete it also triggers a Stored procedure in the Database.

To Install and Uninstall use the folowing commands using Command-Line for Developer in VS.

To Insatall: installutil FileWatcher.exe
To Uninstall: installutil /u FileWatcher.exe

# Coding Guidelines

# Git

I prefer a rebase workflow and **No** feature branches. Most work happens directly on the master branch. For that reason, I recommend setting the pull.rebase setting to true.

`git config --global pull.rebase true`

# Indentation
Please, No space, only TABS.

# Names

1. Use PascalCase for type names
2. Use PascalCase for enum values
3. Use camelCase for function and method names
4. Use camelCase for property names and local variables
5. Use whole words in names when possible
   
# Strings

1. Use "double quotes" for strings shown to the user that need to be externalized (localized)
2. Use 'single quotes' otherwise
3. All strings visible to the user need to be externalized

Base code credit goes to https://github.com/michaelzhang92/FileWatcher
Restore Feature is my work.


## License
[![FOSSA Status](https://app.fossa.io/api/projects/git%2Bgithub.com%2Fhirocsingh%2FFileWatcher-for-DB-Restore.svg?type=large)](https://app.fossa.io/projects/git%2Bgithub.com%2Fhirocsingh%2FFileWatcher-for-DB-Restore?ref=badge_large)