Current Theme Manifest JSON File Structure:

```json
{
    "version": 1,
    "name": "Default",
    "elements": [
        {
            "key": "Menu_Button",
            "base": {
                "fileName": "button",
                "pixelPerUnit": 100,
                "filterMode": 0,
                "borders": {
                    "left": 20,
                    "right": 20,
                    "top": 20,
                    "bottom": 20
                }
            },
            "mask": {
                "fileName": "",
                "pixelPerUnit": 100,
                "filterMode": 1,
                "borders": {
                    "left": 0,
                    "right": 0,
                    "top": 0,
                    "bottom": 0
                }
            },
            "overlay": {
                "fileName": "",
                "pixelPerUnit": 100,
                "filterMode": 1,
                "borders": {
                    "left": 0,
                    "right": 0,
                    "top": 0,
                    "bottom": 0
                }
            }
        },
        {
            "key": "Field_Dark",
            "base": {
                "fileName": "Field_Dark",
                "pixelPerUnit": 100,
                "filterMode": 0,
                "borders": {
                    "left": 20,
                    "right": 20,
                    "top": 20,
                    "bottom": 20
                }
            },
            "mask": {
                "fileName": "",
                "pixelPerUnit": 0,
                "filterMode": 0,
                "borders": {
                    "left": 0,
                    "right": 0,
                    "top": 0,
                    "bottom": 0
                }
            },
            "overlay": {
                "fileName": "",
                "pixelPerUnit": 0,
                "filterMode": 0,
                "borders": {
                    "left": 0,
                    "right": 0,
                    "top": 0,
                    "bottom": 0
                }
            }
        }
    ]
}
```

Key Types:

```c#
public enum Type
{
    Null = 0,
    Menu_Button = 1,
    Big_Panel = 2,
    Mid_Panel = 3,
    Small_Panel = 4,
    Chat_Message = 5,
    Drop_Down_Content = 6,
    Drop_Down_Item = 7,
    Input_Area = 8,
    Text = 9,

}
```
