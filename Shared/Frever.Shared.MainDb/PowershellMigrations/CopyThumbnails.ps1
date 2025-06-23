$destinationBucket ='frever-test'
$sourceBucket='frever-test'

function GetFolders ()
{
    Param ([string] $bucketName)
    $objects = Get-S3Object -BucketName $bucketName 
    $paths=@()

    foreach ($object in $objects) 
    {
        if ($object.Key -like '*Thumbnail*' -AND $object.Key -notlike '*Preloaded*') 
        { 
            $path = split-path $object.Key
            $paths+=$path
        }    
    }
    $paths = $paths | select -Unique
    return $paths
} 
function CopyFile {
    Param ([string]$sourceBucket,[string]$destinationBucket ,[string] $filename,[string] $destFolder)
    $fromPath = 'Preloaded/DefaulThumbnails/'+$filename
    $toPath =$destFolder +'/'+$filename
   
    Copy-S3Object -BucketName $sourceBucket -Key $fromPath -DestinationBucket $destinationBucket -DestinationKey $toPath    
}


$3Icon =@('BodyAnimation','Vfx','Song','Wardrobe','VoiceFilter','FaceAnimation')
$4Icons =@('CharacterSpawnPosition','SetLocation')
$gifTypes = @('BodyAnimation','Vfx','Level','CameraAnimationTemplate')


$file512 ='Thumbnail_512x512.'
$file256 ='Thumbnail_256x256.'
$file128 ='Thumbnail_128x128.'
$file1600 ='Thumbnail_1600x900.'

$folders = GetFolders -bucketName $destinationBucket

foreach ($folder in $folders) 
{ #Assets/<AssetType>/<Id>
    $parts =  $folder.Split('\')
    $assetType = $parts[1]
    $toPath = $folder.replace("\","/")
    
    if($gifTypes.Contains($assetType))
    {
        $extension='gif'
    }
    else
    {
        $extension='png'
    }

    if($3Icon.Contains($assetType))
    {
        #copy 3 icon 
        
        CopyFile -fileName  $file128$extension  -destFolder $toPath  -sourceBucket $sourceBucket -destinationBucket $destinationBucket
        CopyFile -fileName  $file256$extension  -destFolder $toPath  -sourceBucket $sourceBucket -destinationBucket $destinationBucket
        CopyFile -fileName  $file512$extension  -destFolder $toPath  -sourceBucket $sourceBucket -destinationBucket $destinationBucket

    } elseif($4Icons.Contains($assetType)) 
    {
        #copy 4 icon
        
        CopyFile -fileName  $file128$extension  -destFolder $toPath -sourceBucket $sourceBucket -destinationBucket $destinationBucket  
        CopyFile -fileName  $file256$extension  -destFolder $toPath  -sourceBucket $sourceBucket -destinationBucket $destinationBucket 
        CopyFile -fileName  $file512$extension  -destFolder $toPath -sourceBucket $sourceBucket -destinationBucket $destinationBucket  
        CopyFile -fileName  $file1600$extension -destFolder $toPath -sourceBucket $sourceBucket -destinationBucket $destinationBucket  

    } else
    {
        #copy 1 icon
        CopyFile -fileName  $file512$extension  -destFolder $toPath -sourceBucket $sourceBucket -destinationBucket $destinationBucket   
    }
}



