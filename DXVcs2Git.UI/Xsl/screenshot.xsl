<?xml version="1.0"?>
<xsl:stylesheet
    xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
    xmlns:msxsl="urn:schemas-microsoft-com:xslt"
    xmlns:user="http://mycompany.com/mynamespace"
     version="1.0">

    <xsl:output method="html"/>

        <xsl:variable name="cr"><xsl:text>&#xD;</xsl:text></xsl:variable>

<msxsl:script language="JScript" implements-prefix="user">
   var inimage = false;
   function enterimage() {
      inimage = true;
      return '';
   }
   function leaveimage() {
      inimage = false;
      return '';
   }
   function isinimage() {
      return inimage;
   }
</msxsl:script>

        <xsl:template match="/">
        	<xsl:for-each select="/cruisecontrol//buildresults//message">
        		<xsl:if test="user:isinimage()">
	        		<xsl:if test="not(contains(text(), '.'))">
   		    			<xsl:value-of select="text()" />
        				<xsl:value-of select="$cr" />
		        	</xsl:if>
	        		<xsl:if test="contains(text(), '.')">
			        	<xsl:value-of select="user:leaveimage()" />
		        	<xsl:text disable-output-escaping="yes">"/&gt;</xsl:text>
		        	</xsl:if>
	        	</xsl:if>
        		<xsl:if test="contains(text(), 'base64')">
		        	<xsl:value-of select="user:enterimage()" />
		        	<xsl:text disable-output-escaping="yes">&lt;img src="</xsl:text>
        			<xsl:value-of select="text()" />
        			<xsl:value-of select="$cr" />
	        	</xsl:if>
        	</xsl:for-each>

    </xsl:template>
</xsl:stylesheet>
