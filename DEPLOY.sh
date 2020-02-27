# Set these one
UAAS=path/to/project.Web
ASSETS=path/to/project.Frontend/build/assets

# Get current version from SiteSettings.config
BUILD=`xmllint --xpath '/SiteSettings/Settings[1]/assetsFolder/text()' ${UAAS}/Config/SiteSettings.config`

echo "Build version is '$BUILD'."

# Copy assets to Umbraco Cloud repo if the build version exists
# by removing it first and then copying clean assets.
if [[ -e "${UAAS}/assets/${BUILD}" ]]; then

	# Cleanup
	rm -rf "${UAAS}/assets/${BUILD}"
	
	mkdir "${UAAS}/assets/${BUILD}"
	
	# Copy assets to the build directory
	cp $ASSETS/*.* $UAAS/assets/$BUILD

else
	
	# Notify that there's no build directory yet
	echo "$UAAS does not have a '$BUILD' directory."
	
fi
