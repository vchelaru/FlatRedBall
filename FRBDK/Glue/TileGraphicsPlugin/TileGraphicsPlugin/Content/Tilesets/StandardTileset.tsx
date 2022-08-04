<?xml version="1.0" encoding="UTF-8"?>
<tileset version="1.9" tiledversion="1.9.0" name="TiledIcons" tilewidth="16" tileheight="16" tilecount="1024" columns="32">
 <image source="StandardTilesetIcons.png" width="512" height="512"/>
 <tile id="0" class="SolidCollision"/>
 <tile id="1" class="SolidCollision"/>
 <tile id="2" class="SolidCollision"/>
 <tile id="3" class="CloudCollision"/>
 <tile id="4" class="CloudCollision"/>
 <tile id="5" class="CloudCollision"/>
 <tile id="6" class="OneWayCollision"/>
 <tile id="32" class="Water"/>
 <tile id="33" class="BreakableCollision"/>
 <tile id="34" class="IceCollision"/>
 <tile id="64" class="Door"/>
 <tile id="96" class="Ladder"/>
 <tile id="256">
  <properties>
   <property name="MatchType" value="Empty"/>
  </properties>
 </tile>
 <tile id="257">
  <properties>
   <property name="MatchType" value="Ignore"/>
  </properties>
 </tile>
 <tile id="258">
  <properties>
   <property name="MatchType" value="NonEmpty"/>
  </properties>
 </tile>
 <tile id="259">
  <properties>
   <property name="MatchType" value="Other"/>
  </properties>
 </tile>
 <tile id="260">
  <properties>
   <property name="MatchType" value="Negate"/>
  </properties>
 </tile>
 <wangsets>
  <wangset name="CollisionSet" type="mixed" tile="-1">
   <wangcolor name="SolidCollision" color="#ff0000" tile="-1" probability="1"/>
   <wangtile tileid="0" wangid="1,1,1,1,1,1,1,1"/>
   <wangtile tileid="73" wangid="1,1,0,0,0,1,1,1"/>
   <wangtile tileid="74" wangid="1,1,1,1,0,0,0,1"/>
   <wangtile tileid="75" wangid="0,0,0,1,0,0,0,0"/>
   <wangtile tileid="76" wangid="0,0,0,1,1,1,0,0"/>
   <wangtile tileid="77" wangid="0,0,0,0,0,1,0,0"/>
   <wangtile tileid="105" wangid="0,0,0,1,1,1,1,1"/>
   <wangtile tileid="106" wangid="0,1,1,1,1,1,0,0"/>
   <wangtile tileid="107" wangid="0,1,1,1,0,0,0,0"/>
   <wangtile tileid="109" wangid="0,0,0,0,0,1,1,1"/>
   <wangtile tileid="139" wangid="0,1,0,0,0,0,0,0"/>
   <wangtile tileid="140" wangid="1,1,0,0,0,0,0,1"/>
   <wangtile tileid="141" wangid="0,0,0,0,0,0,0,1"/>
  </wangset>
 </wangsets>
</tileset>
