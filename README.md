# Local datasources #


## What ##

We assume people working with Sitecore and reading this know what datasources are. Some of you might even work with a "local" datasources. Local meaning that the datasource item is coupled with the "page" item where it is used in a hierarchical way by storing the datasource item underneath the page item - in a "local" data folder.

### Why ###
Sometimes the solution architect will want to use datasources (don't we want that always?) to enable marketers to use all of Sitecore's features, but some editors find it hard (or just too much work) to create those datasource items. So we tried to automate this process for datasources that should not be shared amongst "pages". 

### How ###
The solution now consists of several parts.


1. The first part will create the actual datasource item (and the data folder if that does not yet exists - datafolder will be pushed as latest child). The template name of the required datasource is used as base for the item name, combined with a number. 
2. A second part will prevent the "Select the associated content" dialog from appearing.
3. In v2.0 a handler was added that will detect if an item was added from a branch template. If so, the code will search for local datasources included in branch items and set the datasource value in the renderings to the newly created items.
4. In v3.0 we did a few bugfixes but also created a solution for final layouts and copying items with local datasources.   
5. v3.1 includes a few more bugfixes and support or branch templates as datasource and a configurable data template (thanks to D. Shevshenko for this)

More indept information on the code will be available on [https://ggullentops.blogspot.com](https://ggullentops.blogspot.com).



## Compatibility ##

> **Important**: this module is **not** compatible with **SXA**

The module was created and tested on Sitecore 8.2 initial release, update-1 and update-2. It works in experience editor only (we assume that people working in the content editor know what they are doing and can create their own datasources...).

As we do override the AddRendering command, the module needs a Sitecore version with the exact same code as the one we used in that command to keep all renderings without local datasources to work as expected.

The Sitecore Experience Accelerator (SXA) has it's own version of the AddRendering command - for the same reason btw. This means that this module should not be installed on instances were SXA is also present!

## Usage ##

### Installation ###

The module is made available on the Sitecore marketplace as a Sitecore package. The package includes:

- a template for the local data folder
- a config file that includes a pipeline processor and a command
- the dll
 
### Renderings ###

You will need to adapt your rendering definition to work with the local datasources. There are 2 settings on the rendering item that are used by the module:

- Datasource Template: is used to create the datasource item from the correct template (and for it's name)
- Datasource Location: the module will create a local datasource only if the location starts with `"./"`.  The remainder of the location will be the name of your datafolder. So you might want to set it to `"./data"`.

### Standard values ###
Sometimes we want to add renderings to the layout on the standard values of a "page" item. But what if that rendering is local? A solution was found based on branch templates: create a **branch** template of the template you want, and add the local rendering there. You can do this with the experience editor and the module will create the local datasource item for you (in the branch). When someone creates a content item based on the branch, the datasource item will also be created. In order to set the datasource value to the newly created item instead of the one in the branch, we added some code to the item:added event in v2.0.     

## History ##
- v1.0 : initial release focussing on XP editor support and creating the datasource items
- v2.0 : added support for standard values through branches (and some bugfixes)
- v3.0 : added support for final layouts and copying items with local datasources (and some bugfixes)
- v3.1 : added support for branches as datasource and made data template configurable


## Future ##

Some ideas did not make it in version 1 and are still "open".. 

1. publish options for data folder and subitems
2. content editor support
4. ...
