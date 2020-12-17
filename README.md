# Umbraco Cloud Default Extras

These are files we use in every project on Umbraco Cloud.

Some C# helpers for use in Razor views:

- Vokseverk.Helpers.cs
- Vokseverk.MediaHelper.cs

A config file with settings for the site across all environments (+ transforms):

- SiteSettings.config
- SiteSettings.development.config
- SiteSettings.live.config

A config transform for the Web.config on LIVE:

- Vokseverk.Web.live.config

A deploy script for copying the frontend build to the Umbraco Cloud site, along
with the C# helpers and the Web.config transform (fun fact: There's an XSLT to
_transform_ the transform from its initial state into the final transform file).

- DEPLOY.sh

## How we use this

We usually start a project with the [vokseverk/project-template](template) repository and rename the project folder, so we will end up with this structure:

	sitename/
	  sitename.Frontend/

Then at some point we'll clone down the Umbraco Cloud project into a sitename.Web
folder, so we get this (the sitename.Web is its own repository - ignored from 
the outer repository):

	sitename/
	  sitename.Frontend/
	  sitename.Web/

And then we'll add this project inside as the `sitename.Core` folder:

	sitename/
	  sitename.Core/
	    App_Code/
	    Config/
	    Transforms/
	  sitename.Frontend/
	  sitename.Web/
	  DEPLOY.sh

This can be done either by specifying the foldername when cloning:

```bash
git clone https://github.com/vokseverk/Vokseverk.CloudExtras.git sitename.Core
```

Or by using the `subtree` command like this:

```bash
git remote add -f EXTRAS https://github.com/vokseverk/Vokseverk.CloudExtras.git
git subtree add --prefix sitename.Core EXTRAS master --squash
```

[template]: https://github.com/vokseverk/project-template/

