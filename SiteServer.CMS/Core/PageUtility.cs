﻿using SiteServer.CMS.Model;
using SiteServer.CMS.Model.Enumerations;
using System;
using SiteServer.CMS.Api.Preview;
using SiteServer.CMS.StlParser.Cache;
using SiteServer.Plugin;
using SiteServer.Utils;
using SiteServer.Utils.Enumerations;

namespace SiteServer.CMS.Core
{
    public static class PageUtility
    {
        public static string GetSiteUrl(SiteInfo siteInfo, bool isLocal)
        {
            return GetSiteUrl(siteInfo, string.Empty, isLocal);
        }

        public static string GetSiteUrl(SiteInfo siteInfo, string requestPath, bool isLocal)
        {
            return isLocal
                ? GetLocalSiteUrl(siteInfo, requestPath)
                : GetRemoteSiteUrl(siteInfo, requestPath);
        }

        public static string GetRemoteSiteUrl(SiteInfo siteInfo)
        {
            return GetRemoteSiteUrl(siteInfo, string.Empty);
        }

        public static string GetRemoteSiteUrl(SiteInfo siteInfo, string requestPath)
        {
            var url = siteInfo.Additional.WebUrl;

            if (string.IsNullOrEmpty(url))
            {
                url = "/";
            }
            else
            {
                if (url != "/" && url.EndsWith("/"))
                {
                    url = url.Substring(0, url.Length - 1);
                }
            }

            if (string.IsNullOrEmpty(requestPath)) return url;

            requestPath = requestPath.Replace(PathUtils.SeparatorChar, PageUtils.SeparatorChar);
            requestPath = PathUtils.RemovePathInvalidChar(requestPath);
            if (requestPath.StartsWith("/"))
            {
                requestPath = requestPath.Substring(1);
            }

            url = PageUtils.Combine(url, requestPath);

            if (!siteInfo.Additional.IsSeparatedAssets) return url;

            var assetsUrl = PageUtils.Combine(siteInfo.Additional.WebUrl,
                siteInfo.Additional.AssetsDir);
            if (StringUtils.StartsWithIgnoreCase(url, assetsUrl))
            {
                url = StringUtils.ReplaceStartsWithIgnoreCase(url, assetsUrl, siteInfo.Additional.AssetsUrl);
            }

            return url;
        }

        public static string GetLocalSiteUrl(SiteInfo siteInfo)
        {
            return GetLocalSiteUrl(siteInfo, string.Empty);
        }

        public static string GetLocalSiteUrl(SiteInfo siteInfo, string requestPath)
        {
            var url = PageUtils.ParseNavigationUrl($"~/{siteInfo.SiteDir}");

            if (string.IsNullOrEmpty(url))
            {
                url = "/";
            }
            else
            {
                if (url != "/" && url.EndsWith("/"))
                {
                    url = url.Substring(0, url.Length - 1);
                }
            }

            if (string.IsNullOrEmpty(requestPath)) return url;

            requestPath = requestPath.Replace(PathUtils.SeparatorChar, PageUtils.SeparatorChar);
            requestPath = PathUtils.RemovePathInvalidChar(requestPath);
            if (requestPath.StartsWith("/"))
            {
                requestPath = requestPath.Substring(1);
            }

            url = PageUtils.Combine(url, requestPath);

            return url;
        }

        public static string GetSiteUrlByPhysicalPath(SiteInfo siteInfo, string physicalPath, bool isLocal)
        {
            if (siteInfo == null)
            {
                var siteId = PathUtility.GetCurrentSiteId();
                siteInfo = SiteManager.GetSiteInfo(siteId);
            }
            if (string.IsNullOrEmpty(physicalPath)) return siteInfo.Additional.WebUrl;

            var publishmentSystemPath = PathUtility.GetSitePath(siteInfo);
            var requestPath = StringUtils.StartsWithIgnoreCase(physicalPath, publishmentSystemPath)
                ? StringUtils.ReplaceStartsWithIgnoreCase(physicalPath, publishmentSystemPath, string.Empty)
                : string.Empty;

            return GetSiteUrl(siteInfo, requestPath, isLocal);
        }

        // 得到发布系统首页地址
        public static string GetIndexPageUrl(SiteInfo siteInfo, bool isLocal)
        {
            var indexTemplateId = TemplateManager.GetIndexTempalteId(siteInfo.Id);
            var createdFileFullName = TemplateManager.GetCreatedFileFullName(siteInfo.Id, indexTemplateId);

            return isLocal
                ? ApiRoutePreview.GetSiteUrl(siteInfo.Id)
                : ParseNavigationUrl(siteInfo, createdFileFullName, false);
        }

        public static string GetSpecialUrl(SiteInfo siteInfo, int specialId, bool isLocal)
        {
            var specialUrl = SpecialManager.GetSpecialUrl(siteInfo, specialId);

            return isLocal
                ? ApiRoutePreview.GetSpecialUrl(siteInfo.Id, specialId)
                : ParseNavigationUrl(siteInfo, specialUrl, false);
        }

        public static string GetFileUrl(SiteInfo siteInfo, int fileTemplateId, bool isLocal)
        {
            var createdFileFullName = TemplateManager.GetCreatedFileFullName(siteInfo.Id, fileTemplateId);

            return isLocal
                ? ApiRoutePreview.GetFileUrl(siteInfo.Id, fileTemplateId)
                : ParseNavigationUrl(siteInfo, createdFileFullName, false);
        }

        public static string GetContentUrl(SiteInfo siteInfo, IContentInfo contentInfo, bool isLocal)
        {
            return GetContentUrlById(siteInfo, contentInfo, isLocal);
        }

        public static string GetContentUrl(SiteInfo siteInfo, ChannelInfo nodeInfo, int contentId, bool isLocal)
        {
            var tableName = ChannelManager.GetTableName(siteInfo, nodeInfo);
            var contentInfo = Content.GetContentInfo(tableName, contentId);
            return GetContentUrlById(siteInfo, contentInfo, isLocal);
        }

        /// <summary>
        /// 对GetContentUrlByID的优化
        /// 通过传入参数contentInfoCurrent，避免对ContentInfo查询太多
        /// </summary>
        private static string GetContentUrlById(SiteInfo siteInfo, IContentInfo contentInfoCurrent, bool isLocal)
        {
            if (contentInfoCurrent == null) return PageUtils.UnclickedUrl;

            if (isLocal)
            {
                return ApiRoutePreview.GetContentUrl(siteInfo.Id, contentInfoCurrent.ChannelId,
                    contentInfoCurrent.Id);
            }

            var sourceId = contentInfoCurrent.SourceId;
            var referenceId = contentInfoCurrent.ReferenceId;
            var linkUrl = contentInfoCurrent.GetString(ContentAttribute.LinkUrl);
            var channelId = contentInfoCurrent.ChannelId;
            if (referenceId > 0 && contentInfoCurrent.GetString(ContentAttribute.TranslateContentType) != ETranslateContentType.ReferenceContent.ToString())
            {
                if (sourceId > 0 && (ChannelManager.IsExists(siteInfo.Id, sourceId) || ChannelManager.IsExists(sourceId)))
                {
                    var targetChannelId = sourceId;
                    var targetSiteId = Node.GetSiteId(targetChannelId);
                    var targetSiteInfo = SiteManager.GetSiteInfo(targetSiteId);
                    var targetChannelInfo = ChannelManager.GetChannelInfo(targetSiteId, targetChannelId);

                    var tableName = ChannelManager.GetTableName(targetSiteInfo, targetChannelInfo);
                    var contentInfo = Content.GetContentInfo(tableName, referenceId);
                    if (contentInfo == null || contentInfo.ChannelId <= 0)
                    {
                        return PageUtils.UnclickedUrl;
                    }
                    if (contentInfo.SiteId == targetSiteInfo.Id)
                    {
                        return GetContentUrlById(targetSiteInfo, contentInfo, false);
                    }
                    var siteInfoTmp = SiteManager.GetSiteInfo(contentInfo.SiteId);
                    return GetContentUrlById(siteInfoTmp, contentInfo, false);
                }
                else
                {
                    var tableName = ChannelManager.GetTableName(siteInfo, channelId);
                    channelId = Content.GetChannelId(tableName, referenceId);
                    linkUrl = Content.GetValue(tableName, referenceId, ContentAttribute.LinkUrl);
                    if (ChannelManager.IsExists(siteInfo.Id, channelId))
                    {
                        return GetContentUrlById(siteInfo, channelId, referenceId, 0, 0, linkUrl, false);
                    }
                    var targetSiteId = Node.GetSiteId(channelId);
                    var targetSiteInfo = SiteManager.GetSiteInfo(targetSiteId);
                    return GetContentUrlById(targetSiteInfo, channelId, referenceId, 0, 0, linkUrl, false);
                }
            }

            if (!string.IsNullOrEmpty(linkUrl))
            {
                return ParseNavigationUrl(siteInfo, linkUrl, false);
            }
            var contentUrl = PathUtility.ContentFilePathRules.Parse(siteInfo, channelId, contentInfoCurrent);
            return GetSiteUrl(siteInfo, contentUrl, false);
        }

        private static string GetContentUrlById(SiteInfo siteInfo, int channelId, int contentId, int sourceId, int referenceId, string linkUrl, bool isLocal)
        {
            if (isLocal)
            {
                return ApiRoutePreview.GetContentUrl(siteInfo.Id, channelId, contentId);
            }

            var tableNameCurrent = ChannelManager.GetTableName(siteInfo, channelId);
            var contentInfoCurrent = Content.GetContentInfo(tableNameCurrent, contentId);

            if (referenceId > 0 && contentInfoCurrent.GetString(ContentAttribute.TranslateContentType) != ETranslateContentType.ReferenceContent.ToString())
            {
                if (sourceId > 0 && (ChannelManager.IsExists(siteInfo.Id, sourceId) || ChannelManager.IsExists(sourceId)))
                {
                    var targetChannelId = sourceId;
                    var targetSiteId = Node.GetSiteId(targetChannelId);
                    var targetSiteInfo = SiteManager.GetSiteInfo(targetSiteId);
                    var targetChannelInfo = ChannelManager.GetChannelInfo(targetSiteId, targetChannelId);

                    var tableName = ChannelManager.GetTableName(targetSiteInfo, targetChannelInfo);
                    var contentInfo = Content.GetContentInfo(tableName, referenceId);
                    if (contentInfo == null || contentInfo.ChannelId <= 0)
                    {
                        return PageUtils.UnclickedUrl;
                    }
                    if (contentInfo.SiteId == targetSiteInfo.Id)
                    {
                        return GetContentUrlById(targetSiteInfo, contentInfo.ChannelId, contentInfo.Id, contentInfo.SourceId, contentInfo.ReferenceId, contentInfo.GetString(ContentAttribute.LinkUrl), false);
                    }
                    var siteInfoTmp = SiteManager.GetSiteInfo(contentInfo.SiteId);
                    return GetContentUrlById(siteInfoTmp, contentInfo.ChannelId, contentInfo.Id, contentInfo.SourceId, contentInfo.ReferenceId, contentInfo.GetString(ContentAttribute.LinkUrl), false);
                }
                else
                {
                    var tableName = ChannelManager.GetTableName(siteInfo, channelId);
                    channelId = Content.GetChannelId(tableName, referenceId);
                    linkUrl = Content.GetValue(tableName, referenceId, ContentAttribute.LinkUrl);
                    return GetContentUrlById(siteInfo, channelId, referenceId, 0, 0, linkUrl, false);
                }
            }
            if (!string.IsNullOrEmpty(linkUrl))
            {
                return ParseNavigationUrl(siteInfo, linkUrl, false);
            }
            var contentUrl = PathUtility.ContentFilePathRules.Parse(siteInfo, channelId, contentId);
            return GetSiteUrl(siteInfo, contentUrl, false);
        }

        private static string GetChannelUrlNotComputed(SiteInfo siteInfo, int channelId, bool isLocal)
        {
            if (channelId == siteInfo.Id)
            {
                return GetIndexPageUrl(siteInfo, isLocal);
            }
            var linkUrl = string.Empty;
            var nodeInfo = ChannelManager.GetChannelInfo(siteInfo.Id, channelId);
            if (nodeInfo != null)
            {
                linkUrl = nodeInfo.LinkUrl;
            }

            if (string.IsNullOrEmpty(linkUrl))
            {
                if (nodeInfo != null)
                {
                    var filePath = nodeInfo.FilePath;

                    if (string.IsNullOrEmpty(filePath))
                    {
                        var channelUrl = PathUtility.ChannelFilePathRules.Parse(siteInfo, channelId);
                        return GetSiteUrl(siteInfo, channelUrl, isLocal);
                    }
                    return ParseNavigationUrl(siteInfo, PathUtility.AddVirtualToPath(filePath), isLocal);
                }
            }

            return ParseNavigationUrl(siteInfo, linkUrl, isLocal);
        }

        //得到栏目经过计算后的连接地址
        public static string GetChannelUrl(SiteInfo siteInfo, ChannelInfo nodeInfo, bool isLocal)
        {
            if (isLocal)
            {
                return ApiRoutePreview.GetChannelUrl(siteInfo.Id, nodeInfo.Id);
            }
            var url = string.Empty;
            if (nodeInfo != null)
            {
                if (nodeInfo.ParentId == 0)
                {
                    url = GetChannelUrlNotComputed(siteInfo, nodeInfo.Id, false);
                }
                else
                {
                    var linkType = ELinkTypeUtils.GetEnumType(nodeInfo.LinkType);
                    if (linkType == ELinkType.None)
                    {
                        url = GetChannelUrlNotComputed(siteInfo, nodeInfo.Id, false);
                    }
                    else if (linkType == ELinkType.NoLink)
                    {
                        url = PageUtils.UnclickedUrl;
                    }
                    else
                    {
                        if (linkType == ELinkType.NoLinkIfContentNotExists)
                        {
                            url = nodeInfo.ContentNum == 0 ? PageUtils.UnclickedUrl : GetChannelUrlNotComputed(siteInfo, nodeInfo.Id, false);
                        }
                        else if (linkType == ELinkType.LinkToOnlyOneContent)
                        {
                            if (nodeInfo.ContentNum == 1)
                            {
                                var tableName = ChannelManager.GetTableName(siteInfo, nodeInfo);
                                //var contentId = StlCacheManager.FirstContentId.GetValue(siteInfo, nodeInfo);
                                var contentId = Content.GetContentId(tableName, nodeInfo.Id, ETaxisTypeUtils.GetContentOrderByString(ETaxisTypeUtils.GetEnumType(nodeInfo.Additional.DefaultTaxisType)));
                                url = GetContentUrl(siteInfo, nodeInfo, contentId, false);
                            }
                            else
                            {
                                url = GetChannelUrlNotComputed(siteInfo, nodeInfo.Id, false);
                            }
                        }
                        else if (linkType == ELinkType.NoLinkIfContentNotExistsAndLinkToOnlyOneContent)
                        {
                            if (nodeInfo.ContentNum == 0)
                            {
                                url = PageUtils.UnclickedUrl;
                            }
                            else if (nodeInfo.ContentNum == 1)
                            {
                                var tableName = ChannelManager.GetTableName(siteInfo, nodeInfo);
                                var contentId = Content.GetContentId(tableName, nodeInfo.Id, ETaxisTypeUtils.GetContentOrderByString(ETaxisTypeUtils.GetEnumType(nodeInfo.Additional.DefaultTaxisType)));
                                //var contentId = StlCacheManager.FirstContentId.GetValue(siteInfo, nodeInfo);
                                url = GetContentUrl(siteInfo, nodeInfo, contentId, false);
                            }
                            else
                            {
                                url = GetChannelUrlNotComputed(siteInfo, nodeInfo.Id, false);
                            }
                        }
                        else if (linkType == ELinkType.LinkToFirstContent)
                        {
                            if (nodeInfo.ContentNum >= 1)
                            {
                                var tableName = ChannelManager.GetTableName(siteInfo, nodeInfo);
                                var contentId = Content.GetContentId(tableName, nodeInfo.Id, ETaxisTypeUtils.GetContentOrderByString(ETaxisTypeUtils.GetEnumType(nodeInfo.Additional.DefaultTaxisType)));
                                //var contentId = StlCacheManager.FirstContentId.GetValue(siteInfo, nodeInfo);
                                url = GetContentUrl(siteInfo, nodeInfo, contentId, false);
                            }
                            else
                            {
                                url = GetChannelUrlNotComputed(siteInfo, nodeInfo.Id, false);
                            }
                        }
                        else if (linkType == ELinkType.NoLinkIfContentNotExistsAndLinkToFirstContent)
                        {
                            if (nodeInfo.ContentNum >= 1)
                            {
                                var tableName = ChannelManager.GetTableName(siteInfo, nodeInfo);
                                var contentId = Content.GetContentId(tableName, nodeInfo.Id, ETaxisTypeUtils.GetContentOrderByString(ETaxisTypeUtils.GetEnumType(nodeInfo.Additional.DefaultTaxisType)));
                                //var contentId = StlCacheManager.FirstContentId.GetValue(siteInfo, nodeInfo);
                                url = GetContentUrl(siteInfo, nodeInfo, contentId, false);
                            }
                            else
                            {
                                url = PageUtils.UnclickedUrl;
                            }
                        }
                        else if (linkType == ELinkType.NoLinkIfChannelNotExists)
                        {
                            url = nodeInfo.ChildrenCount == 0 ? PageUtils.UnclickedUrl : GetChannelUrlNotComputed(siteInfo, nodeInfo.Id, false);
                        }
                        else if (linkType == ELinkType.LinkToLastAddChannel)
                        {
                            var lastAddChannelInfo = Node.GetChannelInfoByLastAddDate(nodeInfo.Id);
                            url = lastAddChannelInfo != null ? GetChannelUrl(siteInfo, lastAddChannelInfo, false) : GetChannelUrlNotComputed(siteInfo, nodeInfo.Id, false);
                        }
                        else if (linkType == ELinkType.LinkToFirstChannel)
                        {
                            var firstChannelInfo = Node.GetChannelInfoByTaxis(nodeInfo.Id);
                            url = firstChannelInfo != null ? GetChannelUrl(siteInfo, firstChannelInfo, false) : GetChannelUrlNotComputed(siteInfo, nodeInfo.Id, false);
                        }
                        else if (linkType == ELinkType.NoLinkIfChannelNotExistsAndLinkToLastAddChannel)
                        {
                            var lastAddChannelInfo = Node.GetChannelInfoByLastAddDate(nodeInfo.Id);
                            url = lastAddChannelInfo != null ? GetChannelUrl(siteInfo, lastAddChannelInfo, false) : PageUtils.UnclickedUrl;
                        }
                        else if (linkType == ELinkType.NoLinkIfChannelNotExistsAndLinkToFirstChannel)
                        {
                            var firstChannelInfo = Node.GetChannelInfoByTaxis(nodeInfo.Id);
                            url = firstChannelInfo != null ? GetChannelUrl(siteInfo, firstChannelInfo, false) : PageUtils.UnclickedUrl;
                        }
                    }
                }
            }
            return url;
        }

        public static string GetInputChannelUrl(SiteInfo siteInfo, ChannelInfo nodeInfo, bool isLocal)
        {
            var channelUrl = GetChannelUrl(siteInfo, nodeInfo, isLocal);
            if (string.IsNullOrEmpty(channelUrl)) return channelUrl;

            channelUrl = StringUtils.ReplaceStartsWith(channelUrl, siteInfo.Additional.WebUrl, string.Empty);
            channelUrl = channelUrl.Trim('/');
            channelUrl = "/" + channelUrl;
            return channelUrl;
        }

        public static string AddVirtualToUrl(string url)
        {
            var resolvedUrl = url;
            if (string.IsNullOrEmpty(url) || PageUtils.IsProtocolUrl(url)) return resolvedUrl;

            if (!url.StartsWith("@") && !url.StartsWith("~"))
            {
                resolvedUrl = PageUtils.Combine("@/", url);
            }
            return resolvedUrl;
        }

        public static string ParseNavigationUrlAddPrefix(SiteInfo siteInfo, string url, bool isLocal)
        {
            if (string.IsNullOrEmpty(url)) return ParseNavigationUrl(siteInfo, url, isLocal);

            if (!url.StartsWith("~/") && !url.StartsWith("@/"))
            {
                url = "@/" + url;
            }
            return ParseNavigationUrl(siteInfo, url, isLocal);
        }

        public static string ParseNavigationUrl(int siteId, string url, bool isLocal)
        {
            var siteInfo = SiteManager.GetSiteInfo(siteId);
            return ParseNavigationUrl(siteInfo, url, isLocal);
        }

        //根据发布系统属性判断是否为相对路径并返回解析后路径
        public static string ParseNavigationUrl(SiteInfo siteInfo, string url, bool isLocal)
        {
            if (siteInfo != null)
            {
                if (!string.IsNullOrEmpty(url) && url.StartsWith("@"))
                {
                    return GetSiteUrl(siteInfo, url.Substring(1), isLocal);
                }
                return PageUtils.ParseNavigationUrl(url);
            }
            return PageUtils.ParseNavigationUrl(url);
        }

        public static string GetVirtualUrl(SiteInfo siteInfo, string url)
        {
            var virtualUrl = StringUtils.ReplaceStartsWith(url, siteInfo.Additional.WebUrl, "@/");
            return StringUtils.ReplaceStartsWith(virtualUrl, "@//", "@/");
        }

        public static bool IsVirtualUrl(string url)
        {
            if (string.IsNullOrEmpty(url)) return false;

            return url.StartsWith("~") || url.StartsWith("@");
        }

        public static string GetSiteFilesUrl(string apiUrl, string relatedUrl)
        {
            if (string.IsNullOrEmpty(apiUrl))
            {
                apiUrl = "/api";
            }
            apiUrl = apiUrl.Trim().ToLower();
            if (apiUrl == "/api")
            {
                apiUrl = "/";
            }
            else if (apiUrl.EndsWith("/api"))
            {
                apiUrl = apiUrl.Substring(0, apiUrl.LastIndexOf("/api", StringComparison.Ordinal));
            }
            else if (apiUrl.EndsWith("/api/"))
            {
                apiUrl = apiUrl.Substring(0, apiUrl.LastIndexOf("/api/", StringComparison.Ordinal));
            }
            if (string.IsNullOrEmpty(apiUrl))
            {
                apiUrl = "/";
            }
            return PageUtils.Combine(apiUrl, DirectoryUtils.SiteFiles.DirectoryName, relatedUrl);
        }

        public static string GetUserFilesUrl(string apiUrl, string relatedUrl)
        {
            return GetSiteFilesUrl(apiUrl, PageUtils.Combine(DirectoryUtils.SiteFiles.UserFiles, relatedUrl));
        }

        public static string GetUserAvatarUrl(string apiUrl, IUserInfo userInfo)
        {
            var imageUrl = userInfo?.AvatarUrl;

            if (!string.IsNullOrEmpty(imageUrl))
            {
                return PageUtils.IsProtocolUrl(imageUrl) ? imageUrl : GetUserFilesUrl(apiUrl, PageUtils.Combine(userInfo.UserName, imageUrl));
            }

            return SiteFilesAssets.GetUrl(apiUrl, "default_avatar.png");
        }
    }
}