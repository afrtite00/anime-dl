import strutils, httpclient, terminal
import ADLCore, ADLCore/genericMediaTypes, ADLCore/Novel/NovelTypes, ADLCore/Video/VideoType
import EPUB/[types, EPUB3]

# TODO: Implement params/commandline arguments.

block:
  type Segment = enum 
                    Quit, Welcome, 
                    Novel, NovelSearch, NovelDownload, NovelUrlInput, 
                    AnimeSelector, Anime, AnimeSearch, AnimeUrlInput, AnimeDownload,
                    Manga, MangaSearch, MangaUrlInput, MangaDownload
  
  var usrInput: string
  var currScraperString: string
  var downBulk: bool
  var curSegment: Segment = Segment.Welcome
  var novelObj: Novel
  var videoObj: Video

  proc SetUserInput() =
    stdout.styledWrite(ForegroundColor.fgGreen, "0 > ")
    usrInput = readLine(stdin)
  proc WelcomeScreen() =
    stdout.styledWriteLine(ForegroundColor.fgRed, "Welcome to anime-dl 3.0")
    stdout.styledWriteLine(ForegroundColor.fgWhite, "\t1) Anime")
    stdout.styledWriteLine(ForegroundColor.fgWhite, "\t2) Novel")
    stdout.styledWriteLine(ForegroundColor.fgWhite, "\t3) Manga")
    while true:
      stdout.styledWrite(ForegroundColor.fgGreen, "0 > ")
      usrInput = readLine(stdin)
      if usrInput.len > 1 or ord(usrInput[0]) <= ord('1') and ord(usrInput[0]) >= ord('3'):
        stdout.styledWriteLine(ForegroundColor.fgRed, "ERR: put isn't 1, 2, 3")
        continue
      if usrInput[0] == '1':
        curSegment = Segment.AnimeSelector
        break
      if usrInput[0] == '2':
        curSegment = Segment.Novel
        break
      if usrInput[0] == '3':
        curSegment = Segment.Manga
        break

  proc NovelScreen() =
    stdout.styledWriteLine(ForegroundColor.fgRed, "novel-dl (Utilizing NovelHall, for now)")
    stdout.styledWriteLine(ForegroundColor.fgWhite, "\t1) Search")
    stdout.styledWriteLine(ForegroundColor.fgWhite, "\t2) Download")
    while true:
      stdout.styledWrite(ForegroundColor.fgGreen, "0 > ")
      usrInput = readLine(stdin)
      if usrInput.len > 1 or ord(usrInput[0]) <= ord('1') and ord(usrInput[0]) >= ord('2'):
        stdout.styledWriteLine(ForegroundColor.fgRed, "ERR: put isn't 1, 2")
        continue
      if usrInput[0] == '1':
        novelObj = GenerateNewNovelInstance("NovelHall", "")
        curSegment = Segment.NovelSearch
        break
      elif usrInput[0] == '2':
        curSegment = Segment.NovelUrlInput
        break
  proc NovelSearchScreen() =
    stdout.styledWrite(ForegroundColor.fgWhite, "Enter Search Term:")
    stdout.styledWrite(ForegroundColor.fgGreen, "0 > ")
    usrInput = readLine(stdin)
    let mSeq = novelObj.searchDownloader(usrInput)
    var idx: int = 0
    var mSa: seq[MetaData]
    if mSeq.len > 9:
      mSa = mSeq[0..9]
    else:
      mSa = mSeq
    for mDat in mSa:
      stdout.styledWriteLine(ForegroundColor.fgGreen, $idx, fgWhite, " | ", fgWhite, mDat.name, " | " & mDat.author)
      inc idx
    while true:
      stdout.styledWriteLine(ForegroundColor.fgWhite, "Select Novel:")
      stdout.styledWrite(ForegroundColor.fgGreen, "0 > ")
      usrInput = readLine(stdin)
      if usrInput.len > 1 or ord(usrInput[0]) <= ord('0') and ord(usrInput[0]) >= ord('8'):
        stdout.styledWriteLine(ForegroundColor.fgRed, "ERR: Doesn't seem to be valid input 0-8")
        continue
      novelObj = GenerateNewNovelInstance("NovelHall", mSeq[parseInt(usrInput)].uri)
      curSegment = Segment.NovelDownload
      break
  proc NovelUrlInputScreen() =
    stdout.styledWriteLine(ForegroundColor.fgWhite, "Paste/Type URL:")
    stdout.styledWrite(ForegroundColor.fgGreen, "0 > ")
    usrInput = readLine(stdin)
    novelObj = GenerateNewNovelInstance("NovelHall",  usrInput)
    curSegment = Segment.NovelDownload
  proc NovelDownloadScreen() =
    discard novelObj.getChapterSequence
    discard novelObj.getMetaData()
    var idx: int = 1
    let mdataList: seq[metaDataList] = @[
      (metaType: MetaType.dc, name: "title", attrs: @[("id", "title")], text: novelObj.metaData.name),
      (metaType: MetaType.dc, name: "creator", attrs: @[("id", "creator")], text: novelObj.metaData.author),
      (metaType: MetaType.dc, name: "language", attrs: @[], text: "en"),
      (metaType: MetaType.meta, name: "", attrs: @[("property", "dcterms:modified")], text: "2022-01-02T03:50:100"),
      (metaType: MetaType.dc, name: "publisher", attrs: @[], text: "animedl")]
    var epub3: EPUB3 = CreateEpub3(mdataList, "./" & novelObj.metaData.name)
    for chp in novelObj.chapters:
      eraseLine()
      stdout.styledWriteLine(fgRed, $idx, "/", $novelObj.chapters.len, " ", fgWhite, chp.name, " ", fgGreen, "Mem: ", $getOccupiedMem(), "/", $getFreeMem())
      cursorUp 1
      let nodes = novelObj.getNodes(chp)
      AddPage(epub3, GeneratePage(chp.name, nodes))
      inc idx
    cursorDown 1
    var coverBytes: string = ""
    try:
      coverBytes = novelObj.ourClient.getContent(novelObj.metaData.coverUri)
    except:
      stdout.styledWriteLine(fgRed, "Could not get novel cover, does it exist?")
    AssignCover(epub3, Image(name: "cover.jpeg", imageType: ImageType.jpeg, bytes: coverBytes))
    FinalizeEpub(epub3)
    curSegment = Segment.Quit

  proc AnimeSelector() =
    stdout.styledWriteLine(fgRed, "Please choose a video scraper!")
    stdout.styledWriteLine(fgWhite, "1) VidStream\t2)HAnime")
    while true:
      SetUserInput()
      if usrInput == "1":
        currScraperString = "vidstreamAni"
      elif usrInput == "2":
        currScraperString = "HAnime"
      else:
        continue
      curSegment = Segment.Anime
      break

  proc AnimeScreen() =
    stdout.styledWriteLine(ForegroundColor.fgRed, "anime-dl ($1)" % [currScraperString])
    stdout.styledWriteLine(ForegroundColor.fgWhite, "\t1) Search")
    stdout.styledWriteLine(ForegroundColor.fgWhite, "\t2) Download (individual)")
    stdout.styledWriteLine(ForegroundColor.fgWhite, "\t3) Download (bulk)")
    while true:
      SetUserInput()
      if usrInput.len > 1 or ord(usrInput[0]) <= ord('1') and ord(usrInput[0]) >= ord('2'):
        stdout.styledWriteLine(ForegroundColor.fgRed, "ERR: put isn't 1, 2")
        continue
      if usrInput[0] == '1':
        videoObj = GenerateNewVideoInstance(currScraperString,  "")
        curSegment = Segment.AnimeSearch
        break
      elif usrInput[0] == '2':
        curSegment = Segment.AnimeUrlInput
        break
      elif usrInput[0] == '3':
        curSegment = Segment.AnimeUrlInput
        downBulk = true
        break
  proc AnimeSearchScreen() =
    stdout.styledWriteLine(ForegroundColor.fgWhite, "Enter Search Term:")
    stdout.styledWrite(ForegroundColor.fgGreen, "0 > ")
    usrInput = readLine(stdin)
    let mSeq = videoObj.searchDownloader(usrInput)
    var idx: int = 0
    var mSa: seq[MetaData]
    if mSeq.len > 9:
      mSa = mSeq[0..9]
    else:
      mSa = mSeq
    for mDat in mSa:
      stdout.styledWriteLine(ForegroundColor.fgGreen, $idx, fgWhite, " | ", fgWhite, mDat.name)
      inc idx
    while true:
      stdout.styledWriteLine(ForegroundColor.fgWhite, "Select Video:")
      SetUserInput()
      if usrInput.len > 1 or ord(usrInput[0]) <= ord('0') and ord(usrInput[0]) >= ord('8'):
        stdout.styledWriteLine(ForegroundColor.fgRed, "ERR: Doesn't seem to be valid input 0-8")
        continue
      videoObj = GenerateNewVideoInstance(currScraperString, mSeq[parseInt(usrInput)].uri)
      discard videoObj.getMetaData()
      discard videoObj.getStream()
      curSegment = Segment.AnimeDownload
      break
  proc AnimeUrlInputScreen() =
    stdout.styledWriteLine(ForegroundColor.fgWhite, "Paste/Type URL:")
    SetUserInput()
    videoObj = GenerateNewVideoInstance(currScraperString,  usrInput)
    discard videoObj.getMetaData()
    discard videoObj.getStream()
    curSegment = Segment.AnimeDownload

  proc loopVideoDownload() =
    stdout.styledWriteLine(fgWhite, "Downloading video for " & videoObj.metaData.name)
    while videoObj.downloadNextVideoPart("./$1.mp4" % [videoObj.metaData.name]):
      eraseLine()
      stdout.styledWriteLine(ForegroundColor.fgWhite, "Got ", ForegroundColor.fgRed, $videoObj.videoCurrIdx, fgWhite, " of ", fgRed, $(videoObj.videoStream.len), " ", fgGreen, "Mem: ", $getOccupiedMem(), "/", $getFreeMem())
      cursorUp 1
    cursorDown 1
    if videoObj.audioStream.len > 0:
      stdout.styledWriteLine(fgWhite, "Downloading audio for " & videoObj.metaData.name)
      while videoObj.downloadNextAudioPart("./$1.ts" % [videoObj.metaData.name]):
        stdout.styledWriteLine(ForegroundColor.fgWhite, "Got ", ForegroundColor.fgRed, $videoObj.audioCurrIdx, fgWhite, " of ", fgRed, $(videoObj.audioStream.len), " ", fgGreen, "Mem: ", $getOccupiedMem(), "/", $getFreeMem())
        cursorUp 1
        eraseLine()
      cursorDown 1
      # TODO: merge formats.

  proc AnimeDownloadScreen() =
    # Not Finalized
    assert videoObj != nil
    if downBulk == false:
      let mStreams: seq[MediaStreamTuple] = videoObj.listResolution()
      var mVid: seq[MediaStreamTuple] = @[]
      var idx: int = 0
      for obj in mStreams:
        if obj.isAudio:
          continue
        else:
          mVid.add(obj)
          stdout.styledWriteLine(ForegroundColor.fgWhite, "$1) $2:$3" % [$len(mVid), obj.id, obj.resolution])
          inc idx
      while true and downBulk == false:
        stdout.styledWriteLine(ForegroundColor.fgWhite, "Please select a resolution:")
        SetUserInput()
        if usrInput.len > 1 or ord(usrInput[0]) <= ord('0') and ord(usrInput[0]) >= ord(($idx)[0]):
          stdout.styledWriteLine(ForegroundColor.fgRed, "ERR: Doesn't seem to be valid input 0-^1")
          continue
        break
      let selMedia = mVid[parseInt(usrInput) - 1]
      videoObj.selResolution(selMedia)
      loopVideoDownload()
    else:
      let mData = videoObj.getEpisodeSequence()
      for meta in mData:
        videoObj = GenerateNewVideoInstance("vidstreamAni", meta.uri)
        discard videoObj.getMetaData()
        discard videoObj.getStream()
        let mResL = videoObj.listResolution()
        var hRes: int = 0
        var indexor: int = 0
        var selector: int = 0
        for res in mResL:
          inc indexor
          let b = parseInt(res.resolution.split('x')[1])
          if b < hRes: continue
          hRes = b
          selector = indexor - 1
        stdout.styledWriteLine(ForegroundColor.fgGreen, "Got resolution: $1 for $2" % [mResL[selector].resolution, videoObj.metaData.name])
        videoObj.selResolution(mResL[selector])
        loopVideoDownload()
    curSegment = Segment.Quit

  proc MangaScreen() =
    stdout.styledWriteLine(ForegroundColor.fgRed, "manga-dl (Utilizing MangaKakalot, for now)")
    stdout.styledWriteLine(ForegroundColor.fgWhite, "\t1) Search")
    stdout.styledWriteLine(ForegroundColor.fgWhite, "\t2) Download")
    while true:
      stdout.styledWrite(ForegroundColor.fgGreen, "0 > ")
      usrInput = readLine(stdin)
      if usrInput.len > 1 or ord(usrInput[0]) <= ord('1') and ord(usrInput[0]) >= ord('2'):
        stdout.styledWriteLine(ForegroundColor.fgRed, "ERR: put isn't 1, 2")
        continue
      if usrInput[0] == '1':
        novelObj = GenerateNewNovelInstance("MangaKakalot", "")
        curSegment = Segment.MangaSearch
        break
      elif usrInput[0] == '2':
        curSegment = Segment.MangaUrlInput
        break
  proc MangaSearchScreen() =
    stdout.styledWrite(ForegroundColor.fgWhite, "Enter Search Term:")
    stdout.styledWrite(ForegroundColor.fgGreen, "0 > ")
    usrInput = readLine(stdin)
    let mSeq = novelObj.searchDownloader(usrInput)
    var idx: int = 0
    var mSa: seq[MetaData]
    if mSeq.len > 9:
      mSa = mSeq[0..9]
    else:
      mSa = mSeq
    for mDat in mSa:
      stdout.styledWriteLine(ForegroundColor.fgGreen, $idx, fgWhite, " | ", fgWhite, mDat.name, " | " & mDat.author)
      inc idx
    while true:
      stdout.styledWriteLine(ForegroundColor.fgWhite, "Select Manga:")
      stdout.styledWrite(ForegroundColor.fgGreen, "0 > ")
      usrInput = readLine(stdin)
      if usrInput.len > 1 or ord(usrInput[0]) <= ord('0') and ord(usrInput[0]) >= ord('8'):
        stdout.styledWriteLine(ForegroundColor.fgRed, "ERR: Doesn't seem to be valid input 0-8")
        continue
      novelObj = GenerateNewNovelInstance("MangaKakalot", mSeq[parseInt(usrInput)].uri)
      curSegment = Segment.MangaDownload
      break
  proc MangaUrlInputScreen() =
    stdout.styledWriteLine(ForegroundColor.fgWhite, "Paste/Type URL:")
    stdout.styledWrite(ForegroundColor.fgGreen, "0 > ")
    usrInput = readLine(stdin)
    novelObj = GenerateNewNovelInstance("MangaKakalot",  usrInput)
    curSegment = Segment.MangaDownload
  proc MangaDownloadScreen() =
    discard novelObj.getChapterSequence
    discard novelObj.getMetaData()
    var idx: int = 1
    let mdataList: seq[metaDataList] = @[
      (metaType: MetaType.dc, name: "title", attrs: @[("id", "title")], text: novelObj.metaData.name),
      (metaType: MetaType.dc, name: "creator", attrs: @[("id", "creator")], text: novelObj.metaData.author),
      (metaType: MetaType.dc, name: "language", attrs: @[], text: "?"),
      (metaType: MetaType.dc, name: "identifier", attrs: @[("id", "pub-id")], text: ""),
      (metaType: MetaType.meta, name: "", attrs: @[("property", "dcterms:modified")], text: "2022-01-02T03:50:100"),
      (metaType: MetaType.dc, name: "publisher", attrs: @[], text: "animedl")]
    var epub3: EPUB3 = CreateEpub3(mdataList, "./" & novelObj.metaData.name)
    for chp in novelObj.chapters:
      eraseLine()
      stdout.styledWriteLine(fgRed, $idx, "/", $novelObj.chapters.len, " ", fgWhite, chp.name, " ", fgGreen, "Mem: ", $getOccupiedMem(), "/", $getFreeMem())
      cursorUp 1
      let nodes = novelObj.getNodes(chp)
      AddPage(epub3, GeneratePage(chp.name, nodes))
      inc idx
    cursorDown 1
    var coverBytes: string = ""
    try:
      coverBytes = novelObj.ourClient.getContent(novelObj.metaData.coverUri)
    except:
      stdout.styledWriteLine(fgRed, "Could not get manga cover, does it exist?")
    AssignCover(epub3, Image(name: "cover.jpeg", imageType: ImageType.jpeg, bytes: coverBytes))
    FinalizeEpub(epub3)
    curSegment = Segment.Quit

  while true:
    case curSegment:
      of Segment.Quit:
        quit(1)
      of Segment.Welcome: WelcomeScreen()
      
      of Segment.Novel: NovelScreen()
      of Segment.NovelSearch: NovelSearchScreen()
      of Segment.NovelUrlInput: NovelUrlInputScreen()
      of Segment.NovelDownload: NovelDownloadScreen()

      of Segment.AnimeSelector: AnimeSelector()
      of Segment.Anime: AnimeScreen()
      of Segment.AnimeSearch: AnimeSearchScreen()
      of Segment.AnimeUrlInput: AnimeUrlInputScreen()
      of Segment.AnimeDownload: AnimeDownloadScreen()
      
      of Segment.Manga: MangaScreen()
      of Segment.MangaSearch: MangaSearchScreen()
      of Segment.MangaUrlInput: MangaUrlInputScreen()
      of Segment.MangaDownload: MangaDownloadScreen()
