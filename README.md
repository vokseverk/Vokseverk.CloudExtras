# UmbracoCloud Default Extras

These are files we use in every project on UmbracoCloud.

Some C# helpers for use in Razor views:

- Vokseverk.Helpers.cs
- Vokseverk.MediaHelper.cs

A config file with settings for the site across all environments (+ transforms):

- SiteSettings.config
- SiteSettings.development.config
- SiteSettings.live.config

A config transform for the Web.config on LIVE:

- Vokseverk.Web.live.config

A deploy script for copying the frontend build to the UmbracoCloud site

- DEPLOY.sh

## How we use this

We usually start a project with the [vokseverk/project-template](template) repository and rename the project folder, so we will end up with this structure:

	sitename/
	  sitename.Frontend/

Then at some point we'll clone down the UmbracoCloud project into a sitename.Web
folder, so we get this (the sitename.Web is its own repository - ignored from 
the outer repository):

	sitename/
	  sitename.Frontend/
	  sitename.Web/

And then we'll add this project inside as well:

	sitename/
	  sitename.Core/
	    App_Code/
	    Config/
	  sitename.Frontend/
	  sitename.Web/
	    Vokseverk.Web.live.config
	  DEPLOY.sh



[template]: https://github.com/vokseverk/project-template/

