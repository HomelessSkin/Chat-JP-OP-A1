Theme Manifest JSON File Structure:

```json
{
  "name":"Test",
  "sprites":
  [
    {
      "key":"Button",
      "fileName":"button.png",
      "pixelPerUnit":100,
      "borders":
      {
        "left":44,
        "right":44,
        "top":44,
        "bottom":44
      }
    }
  ]
}
```
/// Key Types
```c#
public enum Type
{
  Null = 0,
  Button = 1,
  Field_Dark = 2,
  Field_Light = 3,
  Twitch = 4,

}
```
