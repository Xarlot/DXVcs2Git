<?xml version="1.0"?>
<xsl:stylesheet
    xmlns:xsl="http://www.w3.org/1999/XSL/Transform" version="1.0">

    <xsl:output method="html"/>
    <xsl:variable name="cr">
        <xsl:text>
            </xsl:text>
    </xsl:variable>

    <xsl:variable name="help.list" select="/cruisecontrol/build/helpreport/project"/>
    <xsl:variable name="xml.list" select="/cruisecontrol/build/helpreport/xmlfile"/>
    <xsl:variable name="mshc.list" select="/cruisecontrol/build/helpreport/mshcfile"/>
    <xsl:variable name="synclog.list" select="/cruisecontrol/build/helpreport/synclog"/>    
    <xsl:variable name="warnings.list" select="/cruisecontrol/build/helpwarnings/project"/>
    <xsl:variable name="codelength.list" select="/cruisecontrol/build/helpcodelength/project"/>
    <xsl:variable name="codelength.databases" select="/cruisecontrol/build/helpcodelength/testeddatabases"/>

    <xsl:template match="/">
        <xsl:if test="count($help.list) > 0">
            <table class="section-table" cellpadding="0" cellspacing="0" border="0" width="98%">
                <!-- HXS errors -->
                <tr>
                    <td class="sectionheader" colspan="2">
                        Project Errors - <xsl:value-of select="count($help.list)"/> project(s)
                    </td>
                </tr>

                <xsl:apply-templates select="$help.list">
                </xsl:apply-templates>

            </table>
        </xsl:if>

        <xsl:if test="count($xml.list) > 0">
            <table class="section-table" cellpadding="0" cellspacing="0" border="0" width="98%">
                <!-- XML errors -->
                <tr>
                    <td class="sectionheader" colspan="2">
                        Xml Errors - <xsl:value-of select="count($xml.list)"/> file(s)
                    </td>
                </tr>
                <xsl:apply-templates select="$xml.list">
                </xsl:apply-templates>

            </table>
        </xsl:if>

        <xsl:if test="count($mshc.list) > 0">
            <table class="section-table" cellpadding="0" cellspacing="0" border="0" width="98%">
                <!-- MSHC errors -->
                <tr>
                    <td class="sectionheader" colspan="2">
                        Mshc Errors - <xsl:value-of select="count($mshc.list)"/> file(s)
                    </td>
                </tr>
                <xsl:apply-templates select="$mshc.list">
                </xsl:apply-templates>

            </table>
        </xsl:if>

        <xsl:if test="count($synclog.list) > 0">
            <table class="section-table" cellpadding="0" cellspacing="0" border="0" width="98%">
                <!-- Sync log-->
                <tr>
                    <td class="sectionheader" colspan="2">
                        Synchronization results
                    </td>
                </tr>
                <xsl:apply-templates select="$synclog.list">
                </xsl:apply-templates>

            </table>
        </xsl:if>

        <xsl:if test="count($warnings.list) > 0">
            <table class="section-table" cellpadding="0" cellspacing="0" border="0" width="98%">
                <!-- Help warnings -->
                <tr>
                    <td class="sectionheader" colspan="2">
                        Project Warnings - <xsl:value-of select="count($warnings.list)"/> project(s)
                    </td>
                </tr>

                <xsl:apply-templates select="$warnings.list">
                </xsl:apply-templates>

            </table>
        </xsl:if>

        <xsl:if test="count($codelength.list) > 0">
            <table class="section-table" cellpadding="0" cellspacing="0" border="0" width="98%">
                <!-- Help Code Length errors -->
                <tr>
                    <td class="sectionheader" colspan="2">
                        Invalid Code Length - <xsl:value-of select="count($codelength.list)"/> project(s). Tested Databases: <xsl:value-of select="$codelength.databases"/>
                    </td>
                </tr>

                <xsl:apply-templates select="$codelength.list">
                </xsl:apply-templates>

            </table>
        </xsl:if>

    </xsl:template>

    <!-- xml template -->
    <xsl:template match="xmlfile">
        <tr>
            <td class="header-title" valign="top" colspan="2">
                <xsl:value-of select="@name"/>
            </td>
        </tr>
        <tr>
            <td class="section-data">Error:</td>
            <td class="section-error" valign="top">
                <xsl:value-of select="error"/>
            </td>
        </tr>
        <td class="section-data">Details:</td>
        <td class="section-data" valign="top">
            <xsl:for-each select="details">
                <xsl:value-of select="text()" />
                <br />
            </xsl:for-each>
        </td>
        <tr>
            <td colspan="2">
                <hr size="1" width="100%" color="#888888"/>
            </td>
        </tr>
    </xsl:template>

    <!-- mshc template -->
    <xsl:template match="mshcfile">
        <tr>
            <td class="header-title" valign="top" colspan="2">
                <xsl:value-of select="@name"/>
            </td>
        </tr>
        <td class="section-data" valign="top">
            <xsl:for-each select="details">
                <xsl:value-of select="text()" />
                <br />
            </xsl:for-each>
        </td>
        <tr>
            <td colspan="2">
                <hr size="1" width="100%" color="#888888"/>
            </td>
        </tr>
    </xsl:template>

    <!-- synclog template -->
    <xsl:template match="synclog">
        <td class="section-data" valign="top">
            <xsl:for-each select="logline">
                <xsl:value-of select="text()" />
                <br />
            </xsl:for-each>
        </td>
    </xsl:template>

    <!-- project template -->
    <xsl:template match="project">
        <tr>
            <td class="header-title" valign="top" colspan="2">
                <xsl:value-of select="@name"/> (<xsl:value-of select="@errorCount"/> items)
            </td>
            <xsl:apply-templates select="error"/>
            <xsl:apply-templates select="topic"/>
        </tr>
        <tr>
            <td colspan="2">
                <hr size="1" width="100%" color="#888888"/>
            </td>
        </tr>
    </xsl:template>

    <!-- error template -->
    <xsl:template match="error">
        <tr>
            <xsl:if test="position() mod 2=0">
                <xsl:attribute name="class">section-oddrow</xsl:attribute>
            </xsl:if>
            <xsl:if test="position() mod 2!=0">
                <xsl:attribute name="class">section-evenrow</xsl:attribute>
            </xsl:if>

            <xsl:if test="@type">
                <tr>
                    <td class="section-data">Type:</td>
                    <td class="section-error" valign="top">
                        <xsl:value-of select="@type"/>
                    </td>
                </tr>
            </xsl:if>

            <xsl:if test="@name">
                <tr>
                    <td class="section-data">Name:</td>
                    <td class="section-error" valign="top">
                        <xsl:value-of select="@name"/>
                    </td>
                </tr>
            </xsl:if>

            <xsl:if test="@info">
                <tr>
                    <td class="section-data">Info:</td>
                    <td class="section-error" valign="top">
                        <xsl:value-of select="@info"/>
                    </td>
                </tr>
            </xsl:if>

            <xsl:if test="@owner">
                <tr>
                    <td class="section-data">Owner:</td>
                    <td class="section-error" valign="top">
                        <xsl:value-of select="@owner"/>
                    </td>
                </tr>
            </xsl:if>       
            
            <xsl:if test="message">
                <tr>
                    <td class="section-data">Message:</td>
                    <td class="section-error" valign="top">
                        <xsl:value-of select="message"/>
                    </td>
                </tr>
            </xsl:if>

            <xsl:if test="details">
                <tr>
                    <td class="section-data">Details:</td>
                    <td class="section-data" valign="top">
						<xsl:call-template name="br-replace">
                            <xsl:with-param name="word" select="details"/>
                        </xsl:call-template>
                    </td>
                </tr>
            </xsl:if>
        </tr>

        <xsl:if test="position()!= last()">
            <tr>
                <td colspan="2">
                    <hr size="1" width="100%" color="#888888"/>
                </td>
            </tr>
        </xsl:if>
    </xsl:template>

    <!-- topic template -->
    <xsl:template match="topic">
        <tr>
            <xsl:if test="position() mod 2=0">
                <xsl:attribute name="class">section-oddrow</xsl:attribute>
            </xsl:if>
            <xsl:if test="position() mod 2!=0">
                <xsl:attribute name="class">section-evenrow</xsl:attribute>
            </xsl:if>

            <tr>
                <td class="section-data">Topic:</td>
                <td class="section-warning" valign="top">
                    <xsl:value-of select="."/>
                </td>
            </tr>
        </tr>
    </xsl:template>

    <xsl:template name="br-replace">
        <xsl:param name="word"/>
        <xsl:variable name="crD">
            <xsl:text>&#xD;</xsl:text>
        </xsl:variable>
        <xsl:variable name="crA">
            <xsl:text>&#xA;</xsl:text>
        </xsl:variable>
        <xsl:variable name="crAD">
            <xsl:text>
</xsl:text>
        </xsl:variable>
        <xsl:choose>
            <xsl:when test="contains($word,$crAD)">
                <xsl:value-of select="substring-before($word,$crAD)"/>
                <br/>
                <xsl:call-template name="br-replace">
                    <xsl:with-param name="word" select="substring-after($word,$crAD)"/>
                </xsl:call-template>
            </xsl:when>
            <xsl:when test="contains($word,$crA)">
                <xsl:value-of select="substring-before($word,$crA)"/>
                <br/>
                <xsl:call-template name="br-replace">
                    <xsl:with-param name="word" select="substring-after($word,$crA)"/>
                </xsl:call-template>
            </xsl:when>
            <xsl:when test="contains($word,$crD)">
                <xsl:value-of select="substring-before($word,$crD)"/>
                <br/>
                <xsl:call-template name="br-replace">
                    <xsl:with-param name="word" select="substring-after($word,$crD)"/>
                </xsl:call-template>
            </xsl:when>
            <xsl:otherwise>
                <xsl:value-of select="$word"/>
            </xsl:otherwise>
        </xsl:choose>
    </xsl:template>

</xsl:stylesheet>
