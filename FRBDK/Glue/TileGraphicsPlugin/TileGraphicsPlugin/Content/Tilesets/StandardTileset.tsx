<?xml version="1.0" encoding="UTF-8"?>
<tileset version="1.5" tiledversion="1.7.2" name="TiledIcons" tilewidth="16" tileheight="16" tilecount="1024" columns="32">
 <image source="StandardTilesetIcons.png" width="512" height="512"/>
 <tile id="0" type="SolidCollision"/>
 <tile id="1" type="BreakableCollision"/>
 <tile id="2" type="CloudCollision"/>
 <tile id="3" type="OneWayCollision"/>
 <tile id="32" type="Water"/>
 <tile id="33" type="IceCollision"/>
 <tile id="64" type="Door"/>
 <tile id="96" type="Ladder"/>
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
