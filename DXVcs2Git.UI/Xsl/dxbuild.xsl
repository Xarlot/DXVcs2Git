<?xml version="1.0"?>
<xsl:stylesheet
    xmlns:xsl="http://www.w3.org/1999/XSL/Transform" version="1.0">

    <xsl:output method="html"/>

        <xsl:variable name="cr"><xsl:text>
</xsl:text></xsl:variable>

        <xsl:template match="/">
        <xsl:if test="contains(., '[Error]')">
        <xsl:variable name="content">
        	<xsl:for-each select="/cruisecontrol//buildresults//message">
        		<xsl:value-of select="text()" /><xsl:value-of select="$cr" />
        	</xsl:for-each>
        </xsl:variable>
        <table class="section-table" cellpadding="2" cellspacing="0" border="0" width="98%">

            <tr>
                <td class="sectionheader" colspan="2">Build errors:</td>
            </tr>
				<xsl:call-template name="extract-error">
					<xsl:with-param name="word" select="$content"/>
				</xsl:call-template>
        </table>
        </xsl:if>
    </xsl:template>
	<xsl:template name="extract-error">
		<xsl:param name="word"/>
			<xsl:if test="contains($word,'[Error]')">
            <tr>
            <td colspan="2" class="section-error">
                <pre>
                <xsl:value-of select="substring-before(substring-after(substring-after($word,'[Error]--'), $cr), '-------------------------------------------------------------------------')"/></pre>            </td></tr>
				<xsl:call-template name="extract-error">
					<xsl:with-param name="word" select="substring-after(substring-after($word,'[Error]--'), '-------------------------------------------------------------------------')"/>
				</xsl:call-template>
			</xsl:if>
	</xsl:template>
</xsl:stylesheet>
