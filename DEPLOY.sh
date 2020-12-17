# Read the assetsFolder setting from the SiteSettings.config file
# TODO: Find a way to increment the value
# Create a new assets/AxBx/ folder in the .Web project
# Copy the build/assets/ folder from the .Frontend project into the new folder in the .Web project

# TODO: Update PROJECT_SHORTNAME below

# Configuration variables

PROJECT_SHORTNAME="PROJECT_FOLDER_NAME"
PROJECT_ROOT="$TM_PROJECT_DIRECTORY"

# We usually take the development environment's setting
CLOUD_ENV="development"

DEBUG=yes

if [[ $PROJECT_SHORTNAME = "PROJECT_FOLDER_NAME" ]]; then
	echo "You must rename the PROJECT_SHORTNAME environment variable!"
	exit 1
fi

# ================================================ #

WEB_DIR="$PROJECT_ROOT/$PROJECT_SHORTNAME.Web" # The Umbraco Cloud cloned site
FRONTEND_DIR="$PROJECT_ROOT/$PROJECT_SHORTNAME.Frontend" # The frontend files
CORE_DIR="$PROJECT_ROOT/$PROJECT_SHORTNAME.Core" # The Core source files

# Get current asset versions from SiteSettings.config in CORE_DIR
ASSETS_VERSION=`xmllint --xpath "/SiteSettings/Settings[@for='${CLOUD_ENV}']/assetsFolder/text()" ${CORE_DIR}/Config/SiteSettings.config`
ICONS_VERSION=`xmllint --xpath "/SiteSettings/Settings[@for='${CLOUD_ENV}']/iconsVersion/text()" ${CORE_DIR}/Config/SiteSettings.config`

echo "Found $ASSETS_VERSION + $ICONS_VERSION"

DEPLOY_DIR="$WEB_DIR/assets/$ASSETS_VERSION"
ICONS_DIR="$WEB_DIR/$ICONS_VERSION"

BUILD_DIR="$FRONTEND_DIR/build"

if [[ $DEBUG == yes ]]; then
	echo "Deploying to $DEPLOY_DIR"
fi

# Remove the DEPLOY_DIR if it exists already
if [[ -e "$DEPLOY_DIR" ]]; then
	echo "Removing existing builds"
	rm -rf "$DEPLOY_DIR"
	rm -rf "$ICONS_DIR"
fi

# Create the DEPLOY_DIR and ICONS_DIR
echo "Create new $ASSETS_VERSION dir"
mkdir -p "$DEPLOY_DIR/fonts"
mkdir -p "$ICONS_DIR"

# Copy assets
echo "Copy asset files"
cp $BUILD_DIR/assets/*.* "$DEPLOY_DIR"

# Copy fonts
if [[ -d $BUILD_DIR/assets/fonts ]]; then
	echo "Copy font files"
	cp $BUILD_DIR/assets/fonts/*.* "$DEPLOY_DIR/fonts"
fi

if [[ -d "$BUILD_DIR/views" ]]; then
	# Move the "Element" partials first
	# if [[ -e $BUILD_DIR/views/*Block.cshtml ]]; then
		echo "Copy block partials"
		mv $BUILD_DIR/views/*Block.cshtml "$WEB_DIR/Views/Partials/Blocks" 2> /dev/null
	# fi
	
	# Then move the rest
	# if [[ -e $BUILD_DIR/views/*.cshtml ]]; then
		echo "Copy remaining (component) partials"
		mv $BUILD_DIR/views/*.cshtml "$WEB_DIR/Views/Partials/Components" 2> /dev/null
	# fi
fi

if [[ -e "$FRONTEND_DIR/websiteicons" ]]; then
	echo "Copy icons to ICONS_DIR"
	cp $FRONTEND_DIR/websiteicons/*.png $ICONS_DIR
	cp $FRONTEND_DIR/websiteicons/*.json $ICONS_DIR
	# cp $FRONTEND_DIR/websiteicons/*.svg $ICONS_DIR

	echo "Copy favicon and browserconfig to root"
	cp $FRONTEND_DIR/websiteicons/*.xml $WEB_DIR
	cp $FRONTEND_DIR/websiteicons/*.ico $WEB_DIR
fi

# Copy code files & configs from CORE_DIR
cp $CORE_DIR/App_Code/*.cs $WEB_DIR/App_Code
cp $CORE_DIR/Config/*.config $WEB_DIR/Config

xsltproc -o $WEB_DIR/Vokseverk.Web.xdt.config $CORE_DIR/lib/transform.xslt $CORE_DIR/Transforms/Vokseverk.Web.xdt.config

