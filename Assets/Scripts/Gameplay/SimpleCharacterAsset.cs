using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

using UnityEngine;

using Swole;
using Swole.Script;
using Swole.API.Unity;

namespace BulkOut
{

    [CreateAssetMenu(fileName = "SimpleCharacter", menuName = "Bulk Out/SimpleCharacter")]
    public class SimpleCharacterAsset : ScriptableObject
    {
        public void OnValidate()
        {
            if (character != null) character.isInternalAsset = true;
        }

        [SerializeField]
        protected SimpleCharacter character; 
        public SimpleCharacter Character
        {
            get
            {
                if (character != null && !character.IsInitialized && Application.isPlaying) character.Initialize(null); 
                return character;
            }
        }

        public static implicit operator SimpleCharacter(SimpleCharacterAsset asset) => asset.Character;
    }

    [Serializable]
    public class SimpleCharacter : IContent, ICloneable
    {

        public const string prefix_characterFile = "character.";
        public const string prefix_characterIconFile = "characterIcon.";

        public bool isInternalAsset;

        public string originPath;
        public string OriginPath => originPath;
        public IContent SetOriginPath(string path)
        {
            var content = this;
            content.originPath = path;
            return content;
        }
        public string relativePath;
        public string RelativePath => relativePath;
        public IContent SetRelativePath(string path)
        {
            var content = this;
            content.relativePath = path;
            return content;
        }

        public List<PackageIdentifier> ExtractPackageDependencies(List<PackageIdentifier> dependencies = null)
        {
            if (dependencies == null) dependencies = new List<PackageIdentifier>();

            if (assets != null) assets.ExtractPackageDependencies(dependencies);

            return dependencies;
        }

        public override string ToString()
        {
            return $"{Name}{(string.IsNullOrEmpty(Author) ? "" : " created by" + Author)}{(this.HasPackage() ? " - imported from '" + packageInfo.GetIdentityString() + "'" : "")}";
        }

        public string Name => contentInfo.name;

        public string Author => contentInfo.author;
        public string CreationDate => contentInfo.creationDate;
        public string LastEditDate => contentInfo.lastEditDate;
        public string Description => contentInfo.description;

        public PackageInfo packageInfo;
        public PackageInfo PackageInfo => packageInfo;

        public ContentInfo contentInfo;
        public ContentInfo ContentInfo => contentInfo;
        public string SerializedName => prefix_characterFile + contentInfo.name;

        public string CollectionID { get => null; set { } }

        public bool HasCollectionID => false;


        public bool IsInternalAsset { get => isInternalAsset; set => isInternalAsset = value; }

        public bool IsValid => true;

        public Type AssetType => typeof(SimpleCharacterAsset);

        public object Asset => this;

        public SimpleCharacter Duplicate()
        {
            var clone = new SimpleCharacter();

            var fields = typeof(SimpleCharacter).GetFields(BindingFlags.Instance | BindingFlags.Public);
            foreach (var field in fields)
            {
                var val = field.GetValue(clone);
                var innerFields = field.FieldType.GetFields(BindingFlags.Instance | BindingFlags.Public);
                if (val is ICloneable cloneable) val = cloneable.Clone();
                if (!ReferenceEquals(val, null))
                {
                    foreach (var innerField in innerFields)
                    {
                        var val2 = innerField.GetValue(val);
                        if (val2 is ICloneable cloneable2) val2 = cloneable2.Clone();
                        innerField.SetValue(val, val2);
                    }
                }
                field.SetValue(clone, val);
            }

            return clone;
        }
        public object Clone() => Duplicate();

        public IContent CreateCopyAndReplaceContentInfo(ContentInfo info)
        {
            var clone = Duplicate();
            clone.contentInfo = info;
            return clone;
        }

        public IContent CreateShallowCopyAndReplaceContentInfo(ContentInfo info)
        {
            var clone = Duplicate();
            clone.contentInfo = info;
            return clone;
        }

        public void Delete()
        {
        }

        public void Dispose()
        {
        }

        public void DisposeSelf()
        {
        }

        public bool IsIdenticalAsset(ISwoleAsset otherAsset)
        {
            if (ReferenceEquals(this, otherAsset)) return true;
            if (otherAsset is SimpleCharacter character && ReferenceEquals(assets, character.assets)) return true;

            return false;
        }

        public string DisplayName => string.IsNullOrWhiteSpace(alias) ? firstName : alias;

        public string firstName;
        public string lastName;
        public string alias;

        public string description;

        public string icon;

        public SimpleMuscularPlayerAssets assets;

        [SerializeField]
        private Sprite iconSprite;
        public Sprite Icon => iconSprite;

        private bool initialized;
        public bool IsInitialized => initialized;

        public void Initialize(ContentPackage sourcePackage)
        {
            if (sourcePackage != null)
            {
                if (iconSprite == null)
                {
                    if (string.IsNullOrWhiteSpace(icon))
                    {
                        var thumbnailName = $"{prefix_characterIconFile}{Name}";
                        if (sourcePackage != null && sourcePackage.TryFind(out ImageAsset imgAsset, thumbnailName)) iconSprite = imgAsset.Sprite;
                    }
                    else
                    {
                        int firstSeparator = icon.IndexOf(Path.DirectorySeparatorChar);
                        if (firstSeparator < 0) firstSeparator = icon.IndexOf(Path.AltDirectorySeparatorChar);
                        if (firstSeparator >= 0)
                        {
                            string potentialPkgName = icon.Substring(0, firstSeparator);
                            int versionCharIndex = string.IsNullOrWhiteSpace(potentialPkgName) ? -1 : potentialPkgName.IndexOf(SwoleScriptSemantics.ssVersionPrefix);
                            if (versionCharIndex >= 0)
                            {
                                if (ContentManager.TryFindPackage(new PackageIdentifier(potentialPkgName), out var pkg))
                                {
                                    var thumbnailName = Path.GetFileNameWithoutExtension(icon);
                                    if (pkg != null && pkg.TryFind(out ImageAsset imgAsset, thumbnailName)) iconSprite = imgAsset.Sprite;
                                }
                            }
                            else
                            {
                                var thumbnailName = Path.GetFileNameWithoutExtension(icon);
                                if (sourcePackage != null && sourcePackage.TryFind(out ImageAsset imgAsset, thumbnailName)) iconSprite = imgAsset.Sprite;
                            }
                        }
                    }
                }
            }

            initialized = true;
        }

    }

}